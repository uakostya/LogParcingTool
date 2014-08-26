using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogParcer.Model {
	public class Module {
		public string AssemlyPath {
			get;
			set;
		}
		public string AssemblyFullName {
			get;
			set;
		}
		public string TypeName {
			get;
			set;
		}
		public string Caption {
			get;
			set;
		}
	}
}
