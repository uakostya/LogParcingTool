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

        public LogParcerConfigVM(LogAnalisysWindowVM mainVM = null) {
            _mainVM = mainVM;
			Modules = new ObservableCollection<Module>();
			LoadAssemblies();
		}

	    private string GetModulesDirectoryName() {
            var startDir = Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
	        var name = ConfigurationManager.AppSettings["modulesDirectoryName"] ?? "modules";
            if (startDir != null) {
	            var modulesDir = Path.Combine(startDir, name);
	            if (!Directory.Exists(modulesDir)) {
	                Directory.CreateDirectory(modulesDir);
	            }
	            return modulesDir;
	        }
            throw  new ApplicationException("Can't locate startup path");
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
			}
		}

	    private void SetCurrentParcingModule(Module moduleConfig) {
	        if (moduleConfig != null) {
	            var assambly = Assembly.LoadFrom(moduleConfig.AssemlyPath);
	            var instance = (ILogParcer) assambly.CreateInstance(moduleConfig.TypeName);
	            _mainVM.ParcingEngine = instance;
	        }
	        else {
	            _mainVM.ParcingEngine = null;
                _mainVM.LogItems.Clear();
	        }
	    }
	    public static readonly PropertyData CurrentModuleProperty = RegisterProperty("CurrentModule", typeof(Module), typeof(LogParcerConfigVM));
	}
}
