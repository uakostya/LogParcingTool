using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Catel.Collections;
using Catel.Data;
using  Catel.MVVM;
using LogParcer.Common;
using LogParcingUtilities;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.IO;
using ClosedXML.Excel;
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
        private static bool _isConverting = false;

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
            lock (this) {
                if (_isConverting) {
                    CancelCommand.Execute();
                }
                _isConverting = true;
            }
            _currenTokenSource = new CancellationTokenSource();
            LoadingPanel.ShowExcelConvertMessage(_currenTokenSource);
            var options = new ExcelConvertingOptions {
                Directory = dialog.FileName, OutFile = saveDialog.FileName,
                Parcer = p,LogFileName = Settings.LogFileName,
                CancellationToken = _currenTokenSource.Token,
                SearchOption = (Settings.SearchAllDirs)? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly
            };
            if (Settings.UseMinExecTimeInExcell) options.MinExecutionTime = Settings.MinExecutionTimeDecimal;
            await Task.Factory.StartNew(() => LogicUtilities.DoConvertOperation(options),TaskCreationOptions.LongRunning);
            LoadingPanel.HideExcelConvertMessage(() => Process.Start(options.OutFile));
            lock (this) {
                _isConverting = false;
            }
            _currenTokenSource = null;
        }

        

        public async void BrowseLogFilesAndProcess(object sender, RoutedEventArgs e) {
            var ofd = new OpenFileDialog();
            if (ofd.ShowDialog() ?? false) {
                var p = new DBExecutorLogParcer();
                var inf = await Task.Run(() => p.ParceFile(ofd.FileName, p.GetFileConfig()));
                var infVM = new List<LogItemVM>();
                await Task.Run(() => {
                    foreach (var logItem in inf.Where(i => i.Key > Settings.MinExecutionTimeDecimal).ToList()) {
                        infVM.Add(new LogItemVM(logItem.Value));
                    }
                });
                LogItems.AddRange(infVM);
            }
        }

        public readonly Command CancelCommand;

        private void CancelCurrentCommand() {
            if (_currenTokenSource == null) return;
            _currenTokenSource.Cancel();
            _currenTokenSource = null;
        }
    }
}
