using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LogParcer;

namespace Terrasoft.HelloWorld {
    [LogParcingModule("Парсер HelloWorld")]
    public class HelloWorldLogParcer : ILogParcer {
        public SortedList<decimal, LogItem> ParceFile(string fileName, IParcingFileConfig config) {
            var result = Utilities.GetDataContainer();
            result.Add(DateTime.Now.Millisecond, new LogItem{Date = DateTime.Now,ExecutionTime = 42, Level = "Info",Logger = "Dummy",Message = "Hello log parcer"});
            return result;
        }
        public IParcingFileConfig GetFileConfig() {
            return new ParcingFileConfig();
        }
    }

    public class ParcingFileConfig : IParcingFileConfig {
		public ParcingFileConfig() {
			RowSeparator = "Some parameter";
			ColumnSeparator = new char[] {'%'};
		}
        public string RowSeparator { get; set; }
        public char[] ColumnSeparator { get; set; }
        public Encoding Encoding { get; set; }
    }

}
