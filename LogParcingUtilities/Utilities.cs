using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using LogParcer;

namespace LogParcingUtilities
{
    public class DBExecutorLogParcer : ILogParcer {
        public SortedList<decimal,LogItem> ParceFile(string fileName, IParcingFileConfig config) {
            var result = new SortedList<decimal, LogItem>(new DuplicateKeyComparer<decimal>());
            using (TextReader reader = new StreamReader(fileName, config.Encoding)) {
                var sql = new StringBuilder();
                var firstLine = string.Empty;
                string tmpLine = null;
                DateTime tmpDate = default(DateTime);
                while (reader.Peek() > -1) {
                    if (tmpLine != null) {
                        firstLine = tmpLine;
                        tmpLine = null;
                    } else {
                        firstLine = reader.ReadLine();
                    }
                    if (string.IsNullOrWhiteSpace(firstLine)) continue;
                    var row = new LogItem();
                    var appender = string.Empty;
                    var tmpArr = firstLine.Split(config.ColumnSeparator, config.ColumnSplitOptions);
                    if (tmpArr.Length > 4) appender = tmpArr[4];
                    if (appender != config.AppenderName) continue;
                    var exectime = -1m;
                    if (decimal.TryParse(tmpArr[tmpArr.Length - 2], out exectime)) row.ExecutionTime = exectime/1000m;
                    if (tmpDate != default(DateTime)) {
                        row.Date = tmpDate;
                        tmpDate = default(DateTime);
                    } else {
                        row.Date = DateTime.Parse(GetDateString(tmpArr));
                    }
                    sql.Clear();
                    while (reader.Peek()>-1) {
                        tmpLine = reader.ReadLine();
                        if (string.IsNullOrWhiteSpace(tmpLine)) continue;
                        if (tmpLine.Contains(config.AppenderName)) break;
                        tmpArr = tmpLine.Split(config.ColumnSeparator, config.ColumnSplitOptions);
                        if (DateTime.TryParse(GetDateString(tmpArr), out  tmpDate)) {
                            break;
                        }
                        sql.AppendLine(tmpLine);
                    }
                    row.Message = sql.ToString();
                    row.Logger = config.AppenderName;
                    result.Add(row.ExecutionTime,row);
                }
            }
            return result;
        }
        private static string GetDateString(string[] tmpArr) {
            if (tmpArr.Length < 2) return string.Empty;
            var time = tmpArr[1];
            if (string.IsNullOrWhiteSpace(time) || time.Length < 8) return string.Empty;
            return tmpArr.First() + " " + time.Substring(0, 8);
        }
        public IParcingFileConfig GetFileConfig() {
            return new ParcingFileConfig();
        }
    }
    public class ParcingFileConfig : IParcingFileConfig {
        public ParcingFileConfig() {
            ColumnSplitOptions = StringSplitOptions.RemoveEmptyEntries;
            AppenderName = "Terrasoft.Core.DB.DBExecutor";
            Encoding = Encoding.UTF8;
        }
        public string AppenderName { get; set; }
        public char[] ColumnSeparator { get; set; }
        public StringSplitOptions ColumnSplitOptions { get; set; }
        public Encoding Encoding { get; set; }
    }
	[Serializable]
    public class DuplicateKeyComparer<TKey> : IComparer<TKey> where TKey : IComparable {
        #region IComparer<TKey> Members
        public int Compare(TKey x, TKey y) {
            int result = y.CompareTo(x);
            if (result == 0)
                return 1;  
            return result;
        }

        #endregion
    }
}
