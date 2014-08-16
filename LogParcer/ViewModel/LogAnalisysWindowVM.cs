using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        public SettingsVM Settings = InitSettings();
        public string CurrentSqlText { get; set; }

        public LogAnalisysWindowVM() {
            LogItems = new ObservableCollection<LogItemVM>();
            CancelCommand = new Command(cancelCurrentCommand, () => _currenTokenSource != null);
        }
        
        public async void ConvertToExcel(object sender, RoutedEventArgs e) {
            var dialog = new CommonOpenFileDialog {IsFolderPicker = true};
            if (dialog.ShowDialog() != CommonFileDialogResult.Ok) return;
            var saveDialog = new SaveFileDialog {FileName = "report.xlsx"};
            if (!(saveDialog.ShowDialog() ?? false)) return;
            var p = new DBExecutorLogParcer();
            var minExecTime = Settings.MinExecutionTimeDecimal;
            var useMinExecTime = Settings.UseMinExecTimeInExcell;

            lock (this) {
                if (_isConverting) {
                    CancelCommand.Execute();
                }
                _isConverting = true;
            }
            LogicUtilities.DoConvertOperation(dialog, p, useMinExecTime, minExecTime, saveDialog.FileName);
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

        public Command CancelCommand;

        private void cancelCurrentCommand() {
            if (_currenTokenSource != null) {
                _currenTokenSource.Cancel();
                _currenTokenSource = null;
            }
        }
    }
}
