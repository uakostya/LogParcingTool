using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Catel.Data;
using System.IO;
using Catel.MVVM;
using Module = LogParcer.Model.Module;

namespace LogParcer.ViewModel {
	public class LogParcerConfigVM : ViewModelBase {
	    private LogAnalisysWindowVM _mainVM;
		private IParcingFileConfig _config;
        public LogParcerConfigVM(LogAnalisysWindowVM mainVM = null) {
            _mainVM = mainVM;
			Modules = new ObservableCollection<Module>();
			Refresh = new Command(() => {
				Modules.Clear();
				LoadAssemblies();
			});
			LoadAssemblies();
		}

	    private string GetModulesDirectoryName() {
            var startDir = Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
	        var name = ConfigurationManager.AppSettings["modulesDirectoryName"] ?? "modules";
			var modulesDir = Path.Combine(startDir, name);
			if (!Directory.Exists(modulesDir)) {
				Directory.CreateDirectory(modulesDir);
			}
			return modulesDir;
	    }

	    private async void LoadAssemblies() {
			var types = await Task.Run(() => {
				var modules = new List<Module>();
			    var modulesDir = GetModulesDirectoryName();
				foreach (var modulePath in Directory.EnumerateFiles(modulesDir, "*.dll")) {
					var assambly = Assembly.LoadFrom(modulePath);
					foreach (var type in assambly.DefinedTypes) {
						var data = type.GetCustomAttributes(typeof(LogParcingModuleAttribute), false).FirstOrDefault();
						if (data != null) {
							modules.Add(new Module {
								AssemblyFullName = assambly.FullName,
								AssemlyPath = modulePath,
								Caption = ((LogParcingModuleAttribute)data).Caption,
								TypeName = type.FullName
							});
						}
					}
				}
				return modules;
			});
			foreach (var type in types) {
				Modules.Add(type);
			}
		}
			
		public ObservableCollection<Module> Modules {get;set;}
        public Module CurrentModule {
			get {
                return GetValue<Module>(CurrentModuleProperty);
			}
			set {
                SetValue(CurrentModuleProperty, value);
			    SetCurrentParcingModule(value);
				InitConfig();
			}
		}

	    private void SetCurrentParcingModule(Module moduleConfig) {
	        if (moduleConfig != null) {
	            var assambly = Assembly.LoadFrom(moduleConfig.AssemlyPath);
	            var instance = (ILogParcer) assambly.CreateInstance(moduleConfig.TypeName);
	            _mainVM.ParcingEngine = instance;
	        }
	        else {
				Clear();
	        }
	    }
		private void InitConfig() {
			if (_mainVM.ParcingEngine == null)
				return;
			_config = _mainVM.ParcingEngine.GetFileConfig();
			RowSeparator = _config.RowSeparator;
			ColumnSeparator = (_config.ColumnSeparator != null && _config.ColumnSeparator.Length>0)? _config.ColumnSeparator.First().ToString() : "";
		}
		private void Clear() {
			_mainVM.ParcingEngine = null;
			_mainVM.LogItems.Clear();
			RowSeparator = string.Empty;
			ColumnSeparator = string.Empty;
			_config = null;
		}

		public IParcingFileConfig CurrentConfig {
			get {
				_config.RowSeparator = RowSeparator;
				_config.ColumnSeparator = new char[] { !string.IsNullOrEmpty(ColumnSeparator) ? ColumnSeparator.First() : ' ' };
				return _config;
			}
		}

		public Command Refresh {
			get;
			set;
		}
		public string RowSeparator {
			get {
				return GetValue<string>(RowSeparatorProperty);
			}
			set {
				SetValue(RowSeparatorProperty, value);
			}
		}
		public string ColumnSeparator {
			get {
				return GetValue<string>(ColumnSeparatorProperty);
			}
			set {
				SetValue(ColumnSeparatorProperty, value);
			}
		}
	    public static readonly PropertyData CurrentModuleProperty = RegisterProperty("CurrentModule", typeof(Module), typeof(LogParcerConfigVM));
		public static readonly PropertyData RowSeparatorProperty = RegisterProperty("RowSeparator", typeof(string), typeof(LogParcerConfigVM));
		public static readonly PropertyData ColumnSeparatorProperty = RegisterProperty("ColumnSeparator", typeof(string), typeof(LogParcerConfigVM));
	}
}
