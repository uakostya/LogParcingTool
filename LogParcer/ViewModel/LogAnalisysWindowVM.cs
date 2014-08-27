using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Catel.Collections;
using Catel.Data;
using Catel.MVVM;
using LogParcer.Common;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.IO;
using System.Threading;

namespace LogParcer.ViewModel {
    public class LogAnalisysWindowVM : ViewModelBase, IDisposable {
        private CancellationTokenSource _currenTokenSource;
        private static SettingsVM InitSettings() {
            if (File.Exists(SettingsVM.FileName)) {
                var fs = new FileStream(SettingsVM.FileName, FileMode.Open);
                var res = Load<SettingsVM>(fs, SerializationMode.Xml);
                fs.Close();
                return res;
            }
            return new SettingsVM();
        }
        private static bool _isInProcess;
        private ConcurrentDictionary<string, SortedList<decimal, LogItem>> _analizedData;
        private string _currentSortingColumn;
        private bool _listViewIsActual = true;

        public ObservableCollection<LogItemVM> LogItems { get; set; }
        public readonly SettingsVM Settings = InitSettings();
        public readonly LoadingPanelVM LoadingPanel = new LoadingPanelVM();
        public readonly LogParcerConfigVM LogParcerConfig;

        private ILogParcer _parcingEngineInstance;

        public ILogParcer ParcingEngine {
            get { return _parcingEngineInstance; }
            set {
                _parcingEngineInstance = value;
                ParcingEngineInstanced = (_parcingEngineInstance != null);
            }
        }
        public bool ParcingEngineInstanced {
            get { return GetValue<bool>(ParcingEngineInstancedProperty); }
            set {
                SetValue(ParcingEngineInstancedProperty, value);

            }
        }
        public static readonly PropertyData ParcingEngineInstancedProperty = RegisterProperty("ParcingEngineInstanced", typeof(bool), false);

        public LogAnalisysWindowVM() {
            LogItems = new ObservableCollection<LogItemVM>();
            CancelCommand = new Command(CancelCurrentCommand, () => _currenTokenSource != null);
            Browse = new Command(BrowseLogFilesAndProcess);
            OpenLog = new Command(OpenLogFile);
            Export = new Command(ExportToExcel);
            Convert = new Command(ConvertToExcel);
            Load = new Command(LoadViewModel);
            Save = new Command(SaveViewModel);
            RefreshListView = new Command(OnRefreshListView, () => {
                return ((_analizedData != null) && !_listViewIsActual);
            });
            LogParcerConfig = new LogParcerConfigVM(this);
        }

        public Command Convert { get; private set; }
        public async void ConvertToExcel() {
            var dialog = new CommonOpenFileDialog { IsFolderPicker = true };
            if (dialog.ShowDialog() != CommonFileDialogResult.Ok) return;
            var saveDialog = new SaveFileDialog {
                FileName = "report.xlsx", DefaultExt = "xlsx", Filter = "Excel files (*.xlsx)|*.xlsx|All files (*.*)|*.*"
            };
            if (!(saveDialog.ShowDialog() ?? false)) return;
            StopOtherWork();
            LoadingPanel.ShowExcelConvertMessage(_currenTokenSource);
            var options = new ProcessingOptions {
                Directory = dialog.FileName, OutFile = saveDialog.FileName,
                Parcer = ParcingEngine, LogFileName = Settings.LogFileName,
                CancellationToken = _currenTokenSource.Token,
                SearchOption = (Settings.SearchAllDirs) ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly
            };
            if (Settings.UseMinExecTimeInExcell) options.MinExecutionTime = Settings.MinExecutionTimeDecimal;
            await Task.Factory.StartNew(() => LogicUtilities.DoConvertOperation(options), TaskCreationOptions.LongRunning);
            LoadingPanel.HideExcelConvertMessage(() => Process.Start(options.OutFile));
            lock (this) {
                _isInProcess = false;
            }
            _currenTokenSource = null;
        }

        private void StopOtherWork() {
            lock (this) {
                if (_isInProcess) {
                    CancelCommand.Execute();
                }
                _isInProcess = true;
            }
            _currenTokenSource = new CancellationTokenSource();
        }

        public Command OpenLog { get; private set; }
        public async void OpenLogFile() {
            var ofd = new OpenFileDialog();
            if (ofd.ShowDialog() ?? false) {
                StopOtherWork();
                LoadingPanel.ShowImportingMessage(_currenTokenSource, ofd.FileName);
                _analizedData = new ConcurrentDictionary<string, SortedList<decimal, LogItem>>();
                _analizedData[ofd.FileName] = await Task.Run(() => ParcingEngine.ParceFile(ofd.FileName, ParcingEngine.GetFileConfig()));
                var infVM = await GetViewModelCollection();
                LoadingPanel.HideImportingMessage();
                LogItems.Clear();
                LogItems.AddRange(infVM);
            }
        }

        public Command Browse { get; private set; }
        public async void BrowseLogFilesAndProcess() {
            var dialog = new CommonOpenFileDialog { IsFolderPicker = true };
            if (dialog.ShowDialog() != CommonFileDialogResult.Ok) return;
            StopOtherWork();
            LoadingPanel.ShowBrowseLogFilesAndProcessMessage(_currenTokenSource, dialog.FileName);
            var options = new ProcessingOptions {
                Directory = dialog.FileName,
                Parcer = ParcingEngine, LogFileName = Settings.LogFileName,
                CancellationToken = _currenTokenSource.Token,
                SearchOption = (Settings.SearchAllDirs) ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly
            };
            _analizedData = await Task.Run(() => LogicUtilities.LoadFromDir(options));
            var infVM = await GetViewModelCollection();
            LoadingPanel.HideBrowseLogFilesAndProcessMessage();
            LogItems.Clear();
            LogItems.AddRange(infVM);
            infVM.Clear();
        }

        private async Task<List<LogItemVM>> GetViewModelCollection() {
            var infVM = await Task.Run(() => {
                var res = new List<LogItemVM>();
                foreach (var infItem in _analizedData) {
                    res.AddRange(
                        infItem.Value.Where(i => i.Key > Settings.MinExecutionTimeDecimal)
                            .ToList()
                            .Select(logItem => new LogItemVM(logItem.Value)));
                }
                res.Sort((t1, t2) => t1.Date.CompareTo(t2.Date));
                return res;
            }, _currenTokenSource.Token);
            return infVM;
        }

        public Command CancelCommand { get; private set; }
        private void CancelCurrentCommand() {
            if (_currenTokenSource == null) return;
            _currenTokenSource.Cancel();
            _currenTokenSource = null;
        }

        public Command Export { get; private set; }
        public async void ExportToExcel() {
            var saveDialog = new SaveFileDialog {
                FileName = "report.xlsx", DefaultExt = "xlsx", Filter = "Excel files (*.xlsx)|*.xlsx|All files (*.*)|*.*"
            };
            if (!(saveDialog.ShowDialog() ?? false)) return;
            StopOtherWork();
            LoadingPanel.ShowExcelConvertMessage(_currenTokenSource);
            await
                Task.Run(
                    () => LogicUtilities.ExportViewModelToExcel(LogItems.ToList().ConvertAll(i => i.Item),
                            saveDialog.FileName), _currenTokenSource.Token);
            LoadingPanel.HideExcelConvertMessage(() => Process.Start(saveDialog.FileName));
        }

        public Command Load { get; private set; }
        public async void LoadViewModel() {
            var ofd = new OpenFileDialog();
            if (!(ofd.ShowDialog() ?? false)) return;
            StopOtherWork();
            LoadingPanel.ShowLoadMessage(_currenTokenSource, ofd.FileName);
            _analizedData = await Task.Run(() => LogicUtilities.LoadList(ofd.FileName), _currenTokenSource.Token);
            await RefreshListViewItems();
            LoadingPanel.HideMessage();
        }

        private async Task RefreshListViewItems() {
            LogItems.Clear();
            var infVM = await GetViewModelCollection();
            LogItems.AddRange(infVM);
            infVM.Clear();
            _listViewIsActual = true;
        }

        public new Command Save { get; private set; }
        public new async void SaveViewModel() {
            var saveDialog = new SaveFileDialog {
                FileName = "report.bin", DefaultExt = "bin", Filter = "Binary files (*.bin)|*.bin|All files (*.*)|*.*"
            };
            if (!(saveDialog.ShowDialog() ?? false)) return;
            StopOtherWork();
            LoadingPanel.ShowSaveMessage(_currenTokenSource, saveDialog.FileName);
            await Task.Run(() => _analizedData.Save(saveDialog.FileName), _currenTokenSource.Token);
            LoadingPanel.HideSaveMessage(() => Process.Start((new FileInfo(saveDialog.FileName)).DirectoryName));
        }

        public Command RefreshListView { get; private set; }
        public async void OnRefreshListView() {
            await RefreshListViewItems();
        }
        public void MinQueryTimeChanged(object sender, RoutedEventArgs e) {
            _listViewIsActual = false;
        }
        public void LogItemsListView_OnClick(object sender, RoutedEventArgs e) {
            var header = e.OriginalSource as GridViewColumnHeader;
            if (header == null) return;
            var items = LogItems.ToList();
            if (header.Name == _currentSortingColumn) items.Reverse();
            else {
                _currentSortingColumn = header.Name;
                Comparison<LogItemVM> comparsion;
                switch (header.Name) {
                    case "ExecutionTimeHeader":
                        comparsion = (t1, t2) => t1.ExecutionTime.CompareTo(t2.ExecutionTime);
                        break;
                    case "QueryTextHeader":
                        comparsion = (t1, t2) => String.CompareOrdinal(t1.Query, t2.Query);
                        break;
                    default:
                        comparsion = (t1, t2) => t1.Date.CompareTo(t2.Date);
                        break;
                }
                items.Sort(comparsion);
            }
            LogItems.Clear();
            LogItems.AddRange(items);
        }

        #region Члены IDisposable

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool clearManagedResources) {
            if (clearManagedResources) {
                if (_currenTokenSource != null) {
                    _currenTokenSource.Dispose();
                }
            }
        }
        #endregion
    }
}
