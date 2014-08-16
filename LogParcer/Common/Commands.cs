using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace LogParcer.Common {
    public static class Commands {
        public static RoutedCommand BrowseForLogFolder = new RoutedCommand();
        public static RoutedCommand ConvertToExcell = new RoutedCommand();
    }
}
