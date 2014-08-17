using System.IO;
using System.Reflection;
using Catel.Data;
using Catel.Reflection;
using Path = System.IO.Path;

namespace LogParcer.ViewModel {
    class SettingsVM: SavableModelBase<SettingsVM> {
        public static string FileName {
            get {
                const string settingsFileName = "settings.xml";
                var file = Path.Combine(Assembly.GetEntryAssembly().GetDirectory(), settingsFileName);
                return file;
            }
        }

        public void Save() {
            Save(new FileStream(FileName, FileMode.Create), SerializationMode.Xml);
        }
       
        public string MinExecutionTime {
            get { return GetValue<string>(MinExecutionTimeProperty); }
            set { SetValue(MinExecutionTimeProperty, value); }
        }
        public string LogFileName {
            get { return GetValue<string>(LogFileNameProperty); }
            set { SetValue(LogFileNameProperty, value); }
        }

        public decimal MinExecutionTimeDecimal {
            get { return decimal.Parse(MinExecutionTime); }
        }
        public bool UseMinExecTimeInExcell  {
            get { return GetValue<bool>(UseMinExecTimeInExcellProperty); }
            set { SetValue(UseMinExecTimeInExcellProperty, value); }
        }

        public bool SearchAllDirs {
            get { return GetValue<bool>(SearchOptionProperty); }
            set { SetValue(SearchOptionProperty, value); }
        }
        public bool RunExportedFile {
            get { return GetValue<bool>(RunExportedFileProperty); }
            set { SetValue(RunExportedFileProperty, value); }
        }

        public static readonly PropertyData UseMinExecTimeInExcellProperty = RegisterProperty("UseMinExecTimeInExcell", typeof(bool), false);
        public static readonly PropertyData MinExecutionTimeProperty = RegisterProperty("MinExecutionTime", typeof(string), "3");
        public static readonly PropertyData LogFileNameProperty = RegisterProperty("LogFileName", typeof(string), "Common.log");
        public static readonly PropertyData SearchOptionProperty = RegisterProperty("SearchAllDirs", typeof(bool), true);
        public static readonly PropertyData RunExportedFileProperty = RegisterProperty("RunExportedFile", typeof(bool), true);
    }
}
