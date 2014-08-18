using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace LogParcer.ViewModel {
    public static class LogParcingControlsModel {

        private const string HelpFooterTitle = "Press F1 for more help.";
        private static object _lockObject = new object();
        private static Dictionary<string, ControlData> _dataCollection = new Dictionary<string, ControlData>();

        // Store any data that doesnt inherit from ControlData
        private static Dictionary<string, object> _miscData = new Dictionary<string, object>();
        private static void DefaultExecuted() {
        }

        private static bool DefaultCanExecute() {
            return true;
        }

        public static ControlData Open {
            get {
                lock (_lockObject) {
                    string Str = "Open";
                    if (!_dataCollection.ContainsKey(Str)) {
                        string OpenToolTipTitle = "Open (Ctrl+O)";
                        string OpenToolTipDescription = "Open the log file.";

                        ButtonData buttonData = new ButtonData() {
                            Label = Str,
                            SmallImage = new Uri("/LogParcer;component/Images/file.png", UriKind.Relative),
                            ToolTipTitle = OpenToolTipTitle,
                            ToolTipDescription = OpenToolTipDescription,
                            Command = ApplicationCommands.Open,
                            KeyTip = "O",
                        };
                        _dataCollection[Str] = buttonData;
                    }

                    return _dataCollection[Str];
                }
            }
        }
        public static ControlData Browse {
            get {
                lock (_lockObject) {
                    string Str = "Browse";
                    if (!_dataCollection.ContainsKey(Str)) {
                        string BrowseToolTipTitle = "Browse (Ctrl+B)";
                        string BrowseToolTipDescription = "Browse the log files folder.";

                        ButtonData buttonData = new ButtonData() {
                            Label = Str,
                            SmallImage = new Uri("/LogParcer;component/Images/folder.png", UriKind.Relative),
                            ToolTipTitle = BrowseToolTipTitle,
                            ToolTipDescription = BrowseToolTipDescription,
                            Command = Common.Commands.BrowseForLogFolder,
                            KeyTip = "B",
                        };
                        _dataCollection[Str] = buttonData;
                    }

                    return _dataCollection[Str];
                }
            }
        }
        public static ControlData ExportToExcel {
            get {
                lock (_lockObject) {
                    string Str = "ExportToExcel";
                    if (!_dataCollection.ContainsKey(Str)) {
                        string ExportToExcelToolTipTitle = "ExportToExcel (Ctrl+E)";
                        string ExportToExcelToolTipDescription = "ExportToExcel the log files folder.";

                        ButtonData buttonData = new ButtonData() {
                            Label = Str,
                            SmallImage = new Uri("/LogParcer;component/Images/exportExcel.png", UriKind.Relative),
                            ToolTipTitle = ExportToExcelToolTipTitle,
                            ToolTipDescription = ExportToExcelToolTipDescription,
                            Command = Common.Commands.ExportToExcel,
                            KeyTip = "E",
                        };
                        _dataCollection[Str] = buttonData;
                    }

                    return _dataCollection[Str];
                }
            }
        }
        public static ControlData Convert {
            get {
                lock (_lockObject) {
                    const string str = "Convert to excel";
                    if (!_dataCollection.ContainsKey(str)) {
                        const string convertToolTipTitle = "Convert to excel";
                        const string convertToolTipDescription = "Convert the log files to excel.";
                        var buttonData = new ButtonData {
                            Label = str,
                            SmallImage = new Uri("/LogParcer;component/Images/Open_16x16.png", UriKind.Relative),
                            ToolTipTitle = convertToolTipTitle,
                            ToolTipDescription = convertToolTipDescription,
                            Command = Common.Commands.ConvertToExcell,
                            KeyTip = "B",
                        };
                        _dataCollection[str] = buttonData;
                    }
                    return _dataCollection[str];
                }
            }
        }
    }
}
