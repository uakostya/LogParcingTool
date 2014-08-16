using System;
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
using LogParcingUtilities;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.IO;
using ClosedXML.Excel;


namespace LogParcer.ViewModel {
    class LogAnalisysWindowVM : ViewModelBase {
        public ObservableCollection<LogItemVM> LogItems { get; set; }
        public SettingsVM Settings = InitSettings();

        private static SettingsVM InitSettings() {
            if (File.Exists(SettingsVM.FileName)) {
                var fs = new FileStream(SettingsVM.FileName, FileMode.Open);
                var res = Load<SettingsVM>(fs, SerializationMode.Xml);
                fs.Close();
                return res;
            }
            return new SettingsVM();
        }

        public decimal MinExecutionTimeToShow { get; set; }

        public LogAnalisysWindowVM() {
            LogItems = new ObservableCollection<LogItemVM>();
        }
        public  string CurrentSqlText { get; set; }
 
        public void OpenLogFile(object sender, RoutedEventArgs e) {
        
        }
        public async void ConvertToExcel(object sender, RoutedEventArgs e) {
            var dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            if (dialog.ShowDialog() != CommonFileDialogResult.Ok) return;
            var saveDialog = new SaveFileDialog {FileName = "report.xlsx"};
            if (!(saveDialog.ShowDialog() ?? false)) return;
            var p = new DBExecutorLogParcer();
            var config = p.GetFileConfig();
            await Task.Run(() => {
                var excelDoc = new XLWorkbook();
                foreach (var dir in Directory.EnumerateDirectories(dialog.FileName, "*.*", SearchOption.AllDirectories).ToList()) {
                    var sheet = excelDoc.AddWorksheet((new DirectoryInfo(dir)).Name);
                    var file = Path.Combine(dir, Settings.LogFileName);
                    if (File.Exists(file)) {
                        int i = 0;
                        var list = p.ParceFile(file, config);
                        foreach (var row in list.Where(item => item.Key > Settings.MinExecutionTimeDecimal)) {
                            sheet.Cell(++i, 1).Value = row.Value.Date;
                            sheet.Cell(i, 2).Value = row.Value.ExecutionTime;
                            if (row.Value.Query.Length > 32766) {
                                sheet.Cell(i, 3).Value = row.Value.Query.Substring(0, 32766);
                                sheet.Cell(i, 4).Value = row.Value.Query.Substring(32766);
                            } else {
                                sheet.Cell(i, 3).Value = row.Value.Query;
                            }
                            sheet.Row(i).Style.Alignment.SetWrapText(false);
                        }
                    }
                    sheet.Cell(1, 1).WorksheetColumn().AdjustToContents();
                    sheet.Cell(1, 2).WorksheetColumn().AdjustToContents();
                    sheet.Cell(1, 3).WorksheetColumn().AdjustToContents();
                    sheet.Cell(1, 4).WorksheetColumn().AdjustToContents();
                }
                excelDoc.SaveAs(saveDialog.FileName);
                excelDoc.Dispose();
                System.Diagnostics.Process.Start(saveDialog.FileName);
            });
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
    }
}
