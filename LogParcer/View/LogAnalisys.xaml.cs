using System;
using System.Windows;
using System.Windows.Input;
using LogParcer.ViewModel;

namespace LogParcer.View {
    /// <summary>
    /// Interaction logic for LogAnalisys.xaml
    /// </summary>
    public partial class LogAnalisys: IDisposable {
        public readonly LogAnalisysWindowVM CurrentVM = new LogAnalisysWindowVM();
        
        public LogAnalisys() {
            InitializeComponent();
            App.Current.Exit += Current_Exit;
            DataContext = CurrentVM;
            LogItemsListView.ItemsSource = CurrentVM.LogItems;
            SettingsRibbonGroup.DataContext = CurrentVM.Settings;
            ExcelRibbonGroup.DataContext = CurrentVM.Settings;
            LoadingPanel.DataContext = CurrentVM.LoadingPanel;
			MainTab.DataContext = CurrentVM;
        }

        void Current_Exit(object sender, ExitEventArgs e) {
            CurrentVM.Settings.Save();
        }

        private void LogItemsListView_OnClick(object sender, RoutedEventArgs e) {
            CurrentVM.LogItemsListView_OnClick(sender, e);
        }

		private void TextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) {
			CurrentVM.MinQueryTimeChanged(sender, e);
		}

		#region Члены IDisposable

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool clearManagedResources) {
			if (clearManagedResources) {
				if (CurrentVM != null) {
					CurrentVM.Dispose();
				}
			}
		}

		#endregion
	}
}
