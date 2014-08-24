using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Catel.Data;
using Catel.MVVM;
using  Catel.Fody;

namespace LogParcer.ViewModel {
    class LogItemVM : ViewModelBase {

        public LogItemVM(LogItem item) {
            Item = item;
        }

        /// <summary>
        /// Gets or sets the person.
        /// </summary>
        [Model]
        public LogItem Item {
            get { return GetValue<LogItem>(LogItemProperty); }
            private set { SetValue(LogItemProperty, value); }
        }
        public static readonly PropertyData LogItemProperty = RegisterProperty("Item", typeof(LogItem));

        public DateTime Date {
            get { return Item.Date; }
        }
        public Decimal ExecutionTime {
            get { return Item.ExecutionTime; }
        }
        public String Level {
            get { return Item.Level; }
        }
        public String Logger {
            get { return Item.Logger; }
        }
        public String Message {
            get { return Item.Message; }
        }
        public String Query {
            get { return Item.Query; }
        }
    }
}
