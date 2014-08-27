using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Catel.Logging;
using ClosedXML.Excel;
using System.Runtime.Serialization.Formatters.Binary;

namespace LogParcer.Common {
    public static class LogicUtilities {
        private static readonly ILog Log = LogManager.GetLogger(typeof (FileLogListener));
        public static void DoConvertOperation(ProcessingOptions options) {
            IParcingFileConfig config = options.Parcer.GetFileConfig();
            var pOpt = new ParallelOptions {CancellationToken = options.CancellationToken};
            var sheets = new ConcurrentBag<IXLWorksheet>();
            ParallelLoopResult result;
            try {
                result = Parallel.ForEach(
                    Directory.EnumerateDirectories(options.Directory, "*.*", options.SearchOption).ToList(),
                    pOpt,
                    (dir)=> ProcessLogDir(dir, options, config, ref sheets));
            } catch (OperationCanceledException) {
                return;
            } 
            if (!result.IsCompleted) return;
            CreateEntireDocument(options, sheets);
        }

        public static void ExportViewModelToExcel(IEnumerable<LogItem> logItems, string fileName) {
            var book = new XLWorkbook();
            var sheet = book.AddWorksheet(DateTime.Now.ToShortDateString());
            var i = 0;
            foreach (var row in logItems) {
                sheet.Cell(++i, 1).Value = row.Date;
                sheet.Cell(i, 2).Value = row.ExecutionTime;
                if (row.Message.Length < 32766) {
					sheet.Cell(i, 3).Value = row.Message;
                }
                sheet.Row(i).Style.Alignment.SetWrapText(false);
            }
            sheet.Cell(1, 1).WorksheetColumn().AdjustToContents();
            sheet.Cell(1, 2).WorksheetColumn().AdjustToContents();
            sheet.Cell(1, 3).WorksheetColumn().AdjustToContents();
            book.SaveAs(fileName);
        }

        public static bool Save(this ConcurrentDictionary<string, SortedList<decimal, LogItem>> items, string path) {
            var formatter = new BinaryFormatter();
            using (var fs = new FileStream(path,FileMode.Create)) {
                try {
                    formatter.Serialize(fs, items);
                    return true;
                }
                catch (IOException ex) {
                    Log.Error(ex, "{0}", Application.Current.FindResource("IO.Write.Error"));
                    return false;
                }
            }
        }
        public static ConcurrentDictionary<string, SortedList<decimal, LogItem>> LoadList(string path) {
            var formatter = new BinaryFormatter();
            try {
                using (var fs = new FileStream(path, FileMode.Open)) {
                    return (ConcurrentDictionary<string, SortedList<decimal, LogItem>>)formatter.Deserialize(fs);
                }
            } catch (IOException ex) {
                Log.Error(ex, "{0}", Application.Current.FindResource("IO.Read.Error"));
                return new ConcurrentDictionary<string, SortedList<decimal, LogItem>>();
            } 
        }
        public static ConcurrentDictionary<string, SortedList<decimal, LogItem>> LoadFromDir(ProcessingOptions options) {
            IParcingFileConfig config = options.Parcer.GetFileConfig();
            var result = new ConcurrentDictionary<string, SortedList<decimal, LogItem>>(); 
            var pOpt = new ParallelOptions { CancellationToken = options.CancellationToken };
            var dirList = Directory.EnumerateDirectories(options.Directory, "*.*", options.SearchOption).ToList();
            var lists = new ConcurrentDictionary<string ,SortedList<decimal, LogItem>>();
            ParallelLoopResult loopResult;
            try {
                loopResult = Parallel.ForEach(dirList, pOpt, (dir) => {
                        var file = Path.Combine(dir, options.LogFileName);
                        if (File.Exists(file)) {
                            var list = options.Parcer.ParceFile(file, config);
                            lists[file] = list;
                        }
                    });
            } catch (OperationCanceledException) {
                return result;
            }
            if (!loopResult.IsCompleted) return result;
            return lists;
        }
        
        private static void CreateEntireDocument(ProcessingOptions options, IEnumerable<IXLWorksheet> sheets) {
            var excelDoc = new XLWorkbook();
            foreach (var sheet in sheets) {
                excelDoc.AddWorksheet(sheet);
            }
            excelDoc.SaveAs(options.OutFile);
            excelDoc.Dispose();
        }
        private static void ProcessLogDir(string dir, ProcessingOptions options, IParcingFileConfig config, ref ConcurrentBag<IXLWorksheet> sheets) {
            var sheet = (new XLWorkbook()).AddWorksheet((new DirectoryInfo(dir)).Name);
            var file = Path.Combine(dir, options.LogFileName);
            ProcessLogFile(options, config, file, (row, i) => {
                sheet.Cell(++i, 1).Value = row.Value.Date;
                sheet.Cell(i, 2).Value = row.Value.ExecutionTime;
				if (row.Value.Message.Length < 32766) {
					sheet.Cell(i, 3).Value = row.Value.Message;
                }
                sheet.Row(i).Style.Alignment.SetWrapText(false);
                return i;
            });
            sheet.Cell(1, 1).WorksheetColumn().AdjustToContents();
            sheet.Cell(1, 2).WorksheetColumn().AdjustToContents();
            sheet.Cell(1, 3).WorksheetColumn().AdjustToContents();
            sheets.Add(sheet);
        }
        private static void ProcessLogFile(ProcessingOptions options, IParcingFileConfig config, string file
            , Func<KeyValuePair<decimal, LogItem>, int, int> rowAction) {
            if (File.Exists(file)) {
                var list = options.Parcer.ParceFile(file, config);
                var listItems = (options.MinExecutionTime.HasValue)
                    ? list.Where(item => item.Key > (decimal)options.MinExecutionTime)
                    : list;
                listItems.Aggregate(0, (current, row) => rowAction(row, current));
            }
        }
    }

   public class  ProcessingOptions {
       public decimal? MinExecutionTime { get; set; }
       public string LogFileName { get; set; }
       public string Directory { get; set; }
       public string OutFile { get; set; }
       public CancellationToken CancellationToken { get; set; }
       public ILogParcer Parcer { get; set; }
       public SearchOption SearchOption { get; set; }
    }
}
