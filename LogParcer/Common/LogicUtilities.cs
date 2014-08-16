using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ClosedXML.Excel;
using LogParcer.ViewModel;
using LogParcingUtilities;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace LogParcer.Common {
    public static class LogicUtilities {
        public static void DoConvertOperation(ExcelConvertingOptions options) {
            IParcingFileConfig config = options.Parcer.GetFileConfig();
            var excelDoc = new XLWorkbook();
            var pOpt = new ParallelOptions {CancellationToken = options.CancellationToken};
            var sheets = new ConcurrentBag<IXLWorksheet>();
            var result = new ParallelLoopResult();
            try {
                result = Parallel.ForEach(
                    Directory.EnumerateDirectories(options.Directory, "*.*", SearchOption.AllDirectories).ToList(),
                    pOpt,
                    (dir) => {
                        var sheet = (new XLWorkbook()).AddWorksheet((new DirectoryInfo(dir)).Name);
                        var file = Path.Combine(dir, options.LogFileName);
                        if (File.Exists(file)) {
                            int i = 0;
                            var list = options.Parcer.ParceFile(file, config);
                            var listItems = (options.MinExecutionTime.HasValue)
                                ? list.Where(item => item.Key > (decimal)options.MinExecutionTime)
                                : list;
                            foreach (var row in listItems) {
                                sheet.Cell(++i, 1).Value = row.Value.Date;
                                sheet.Cell(i, 2).Value = row.Value.ExecutionTime;
                                if (row.Value.Query.Length < 32766) {
                                    sheet.Cell(i, 3).Value = row.Value.Query;
                                }
                                sheet.Row(i).Style.Alignment.SetWrapText(false);
                            }
                        }
                        sheet.Cell(1, 1).WorksheetColumn().AdjustToContents();
                        sheet.Cell(1, 2).WorksheetColumn().AdjustToContents();
                        sheet.Cell(1, 3).WorksheetColumn().AdjustToContents();
                        sheets.Add(sheet);
                    });
            } catch (OperationCanceledException) {
                return;
            } 
            if (!result.IsCompleted) return;
            foreach (var sheet in sheets) {
                excelDoc.AddWorksheet(sheet);
            }
            excelDoc.SaveAs(options.OutFile);
            excelDoc.Dispose();
            if (options.RunExportedFile) System.Diagnostics.Process.Start(options.OutFile);
        }
    }

   public class  ExcelConvertingOptions {
        public decimal? MinExecutionTime { get; set; }
        public string LogFileName { get; set; }
        public string Directory { get; set; }
        public string OutFile { get; set; }
        public CancellationToken CancellationToken { get; set; }
       public bool RunExportedFile { get; set; }
       public DBExecutorLogParcer Parcer { get; set; }
    }
}
