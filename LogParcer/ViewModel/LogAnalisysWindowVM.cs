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
using LogParcingUtilities;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.IO;
using System.Threading;

namespace LogParcer.ViewModel {
    class LogAnalisysWindowVM : ViewModelBase {
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

        public ObservableCollection<LogItemVM> LogItems { get; set; }
        public readonly SettingsVM Settings = InitSettings();
        public readonly LoadingPanelVM LoadingPanel = new LoadingPanelVM();

        public LogAnalisysWindowVM() {
            LogItems = new ObservableCollection<LogItemVM>();
            CancelCommand = new Command(CancelCurrentCommand, () => _currenTokenSource != null);
        }
        
        public async void ConvertToExcel(object sender, RoutedEventArgs e) {
            var dialog = new CommonOpenFileDialog {IsFolderPicker = true};
            if (dialog.ShowDialog() != CommonFileDialogResult.Ok) return;
            var saveDialog = new SaveFileDialog {
                FileName = "report.xlsx", DefaultExt = "xlsx", Filter = "Excel files (*.xlsx)|*.xlsx|All files (*.*)|*.*"
            };
            if (!(saveDialog.ShowDialog() ?? false)) return;
            var p = new DBExecutorLogParcer();
            StopOtherWork();
            LoadingPanel.ShowExcelConvertMessage(_currenTokenSource);
            var options = new ProcessingOptions {
                Directory = dialog.FileName, OutFile = saveDialog.FileName,
                Parcer = p,LogFileName = Settings.LogFileName,
                CancellationToken = _currenTokenSource.Token,
                SearchOption = (Settings.SearchAllDirs)? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly
            };
            if (Settings.UseMinExecTimeInExcell) options.MinExecutionTime = Settings.MinExecutionTimeDecimal;
            await Task.Factory.StartNew(() => LogicUtilities.DoConvertOperation(options),TaskCreationOptions.LongRunning);
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

        public async void OpenLogFile (object sender, RoutedEventArgs e) {
            var ofd = new OpenFileDialog();
            if (ofd.ShowDialog() ?? false) {
                StopOtherWork();
                LoadingPanel.ShowImportingMessage(_currenTokenSource, ofd.FileName);
                var p = new DBExecutorLogParcer();
                var inf = await Task.Run(() => p.ParceFile(ofd.FileName, p.GetFileConfig()));
                var infVM = new List<LogItemVM>();
                await Task.Run(() => infVM.AddRange(inf.Where(i => i.Key > Settings.MinExecutionTimeDecimal).ToList().Select(logItem => new LogItemVM(logItem.Value))),
                    _currenTokenSource.Token);
                LoadingPanel.HideImportingMessage();
                LogItems.Clear();
                LogItems.AddRange(infVM);
            }
        }
        public async void BrowseLogFilesAndProcess(object sender, RoutedEventArgs e) {
            var dialog = new CommonOpenFileDialog { IsFolderPicker = true };
            if (dialog.ShowDialog() != CommonFileDialogResult.Ok) return;
            StopOtherWork();
            LoadingPanel.ShowBrowseLogFilesAndProcessMessage(_currenTokenSource, dialog.FileName);
            var options = new ProcessingOptions {
                Directory = dialog.FileName, 
                Parcer = new DBExecutorLogParcer(), LogFileName = Settings.LogFileName,
                CancellationToken = _currenTokenSource.Token,
                SearchOption = (Settings.SearchAllDirs) ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly
            };
            _analizedData = await Task.Run(() => LogicUtilities.LoadFromDir(options));
            var infVM = new List<LogItemVM>();
            await Task.Run(() => {
                foreach (var infItem in _analizedData) {
                    infVM.AddRange(
                        infItem.Value.Where(i => i.Key > Settings.MinExecutionTimeDecimal)
                            .ToList()
                            .Select(logItem => new LogItemVM(logItem.Value)));
                }
                infVM.Sort((t1,t2)=>t1.Date.CompareTo(t2.Date));
            }, _currenTokenSource.Token);
            LoadingPanel.HideBrowseLogFilesAndProcessMessage();
            LogItems.Clear();
            LogItems.AddRange(infVM);
            infVM.Clear();
        }

        public readonly Command CancelCommand;
        private void CancelCurrentCommand() {
            if (_currenTokenSource == null) return;
            _currenTokenSource.Cancel();
            _currenTokenSource = null;
        }

        public async void ExportToExcel(object sender, RoutedEventArgs e) {
            var saveDialog = new SaveFileDialog {
                FileName = "report.xlsx", DefaultExt = "xlsx", Filter = "Excel files (*.xlsx)|*.xlsx|All files (*.*)|*.*"
            };
            if (!(saveDialog.ShowDialog() ?? false)) return;
            StopOtherWork();
            LoadingPanel.ShowExcelConvertMessage(_currenTokenSource);
            await
                Task.Run(
                    () =>
                        LogicUtilities.ExportViewModelToExcel(LogItems.ToList().ConvertAll(i => i.Item),
                            saveDialog.FileName), _currenTokenSource.Token);
            LoadingPanel.HideExcelConvertMessage(() => Process.Start(saveDialog.FileName));
        }
        public void LogItemsListView_OnClick (object sender, RoutedEventArgs e) {
            var header = e.OriginalSource as GridViewColumnHeader;
            if (header == null)return;
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
                        comparsion = (t1, t2) => String.CompareOrdinal(t1.Query,t2.Query);
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
    }
}
