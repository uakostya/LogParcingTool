using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using LogParcer.ViewModel;
using List = DocumentFormat.OpenXml.Office2010.ExcelAc.List;

namespace LogParcer.View {
    /// <summary>
    /// Interaction logic for LogAnalisys.xaml
    /// </summary>
    public partial class LogAnalisys {
        readonly LogAnalisysWindowVM _mainVm = new LogAnalisysWindowVM();
        
        public LogAnalisys() {
            InitializeComponent();
            App.Current.Exit += Current_Exit;
            DataContext = _mainVm;
            LogItemsListView.ItemsSource = _mainVm.LogItems;
            var bindingOpen = new CommandBinding(ApplicationCommands.Open);
            bindingOpen.Executed += _mainVm.OpenLogFile;
            CommandBindings.Add(bindingOpen);
            var bindingBrowse = new CommandBinding(Common.Commands.BrowseForLogFolder);
            bindingBrowse.Executed += _mainVm.BrowseLogFilesAndProcess;
            CommandBindings.Add(bindingBrowse);
            var bindingConvertToExcell = new CommandBinding(Common.Commands.ConvertToExcell);
            bindingConvertToExcell.Executed += _mainVm.ConvertToExcel;
            CommandBindings.Add(bindingConvertToExcell);
            SettingsRibbonGroup.DataContext = _mainVm.Settings;
            ExcelRibbonGroup.DataContext = _mainVm.Settings;
        }

        void Current_Exit(object sender, ExitEventArgs e) {
            _mainVm.Settings.Save();
        }
    }
}
