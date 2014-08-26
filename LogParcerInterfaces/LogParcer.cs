using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace LogParcer {
	[Serializable]
    public class LogItem {
        public string Level { get; set; }
        public DateTime Date { get; set; }
        public decimal ExecutionTime { get; set; }
        public string Logger { get; set; }
        public string Message { get; set; }
    }

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
}
