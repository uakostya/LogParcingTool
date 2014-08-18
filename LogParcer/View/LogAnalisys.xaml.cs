using System.Windows;
using System.Windows.Input;
using LogParcer.ViewModel;

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
            var bindingExport = new CommandBinding(Common.Commands.ExportToExcel);
            bindingExport.Executed += _mainVm.ExportToExcel;
            CommandBindings.Add(bindingExport);
            SettingsRibbonGroup.DataContext = _mainVm.Settings;
            ExcelRibbonGroup.DataContext = _mainVm.Settings;
            LoadingPanel.DataContext = _mainVm.LoadingPanel;
        }

        void Current_Exit(object sender, ExitEventArgs e) {
            _mainVm.Settings.Save();
        }

        private void LogItemsListView_OnClick(object sender, RoutedEventArgs e) {
            _mainVm.LogItemsListView_OnClick(sender, e);
        }
    }
}
