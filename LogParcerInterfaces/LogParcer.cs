using System;
using System.Collections.Generic;
using System.Text;

// ReSharper disable once CheckNamespace
namespace LogParcer {
    public interface IParcingFileConfig {
        string AppenderName { get; set; }
        char[] ColumnSeparator { get; set; }
        StringSplitOptions ColumnSplitOptions { get; set; }
        Encoding Encoding { get; set; }
    }
    public interface ILogParcer {
        SortedList<decimal, LogItem> ParceFile(string fileName, IParcingFileConfig config);
        IParcingFileConfig GetFileConfig();
    }
    
    [Serializable]
    public class LogItem {
        public string Level { get; set; }
        public DateTime Date { get; set; }
        public decimal ExecutionTime { get; set; }
        public string Logger { get; set; }
        public string Message { get; set; }
    }

	[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple =true)]
	public sealed class LogParcingModuleAttribute : Attribute {
		readonly string caption;
		public LogParcingModuleAttribute(string caption) {
			this.caption = caption;
		}
		public string Caption {
			get {
				return caption;
			}
		}
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

    public static class Utilities {
        /// <summary>
        /// Returns container collection for log items
        /// </summary>
        /// <param name="customComparer">Must have [Serializable] attirute</param>
        /// <returns></returns>
        public static SortedList<decimal, LogItem> GetDataContainer( IComparer<decimal> customComparer = null) {
            return new SortedList<decimal, LogItem>(customComparer ?? new DuplicateKeyComparer<decimal>());
        }
    }
}
