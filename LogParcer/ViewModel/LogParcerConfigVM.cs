using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Catel.Data;
using System.IO;
using Catel.MVVM;
using LogParcer.Model;

namespace LogParcer.ViewModel {
	public class LogParcerConfigVM : ViewModelBase {
		public LogParcerConfigVM() {
			Modules = new ObservableCollection<Module>();
			LoadAssemblies();
		}
		private async void LoadAssemblies() {
			var types = await Task.Run(() => {
				var modules = new List<Module>();
				var startDir = Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
				var modulesDir = Path.Combine(startDir, "modules");
				foreach (var modulePath in Directory.EnumerateFiles(modulesDir, "*.dll")) {
					var assambly = System.Reflection.Assembly.LoadFrom(modulePath);
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
		public string CurrentAssembly {
			get {
				return GetValue<string>(CurrentAssemblyProperty);
			}
			set {
				SetValue(CurrentAssemblyProperty, value);
			}
		}

		public static readonly PropertyData CurrentAssemblyProperty = RegisterProperty("CurrentAssembly", typeof(string), typeof(LogParcerConfigVM));
	}
}
