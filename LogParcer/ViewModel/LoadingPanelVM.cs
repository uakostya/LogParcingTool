using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Catel.Data;
using Catel.MVVM;


namespace LogParcer.ViewModel {
	public class LoadingPanelVM : ViewModelBase {
        public LoadingPanelVM() {
            ClosePanelCommand = new Command(ClosePanelCommandExecute);
            CommandToRun = new Command(CommandToRunExecute);
        }
        private CancellationTokenSource _currentCancellationTokenSource;
        private Action _commandToRunAction;
        public bool IsLoading {
            get { return GetValue<bool>(IsLoadingProperty); }
            set { SetValue(IsLoadingProperty, value); }
        }
        public bool HaveCommandToRun {
            get { return GetValue<bool>(HaveCommandToRunProperty); }
            set { SetValue(HaveCommandToRunProperty, value); }
        }
        public string Message {
            get { return GetValue<string>(MessageProperty); }
            set { SetValue(MessageProperty, value); }
        }
        public string SubMessage {
            get { return GetValue<string>(SubMessageProperty); }
            set { SetValue(SubMessageProperty, value); }
        }
        public Command CommandToRun { get; private set; }
        
        public Command ClosePanelCommand { get; private set; }
        private void ClosePanelCommandExecute() {
            if (_currentCancellationTokenSource != null) {
                _currentCancellationTokenSource.Cancel();
            }
            IsLoading = false;
        }
        private void CommandToRunExecute() {
            if (_commandToRunAction != null) {
                _commandToRunAction();
            }
            IsLoading = false;
        }

        
        // ReSharper disable MemberCanBePrivate.Global
        public static readonly PropertyData IsLoadingProperty = RegisterProperty("IsLoading", typeof(bool), false);
        public static readonly PropertyData MessageProperty = RegisterProperty("Message", typeof(string), "Hello");
        public static readonly PropertyData SubMessageProperty = RegisterProperty("SubMessage", typeof(string), "world");
        public static readonly PropertyData HaveCommandToRunProperty = RegisterProperty("HaveCommandToRun", typeof(bool), false);
        
        // ReSharper restore MemberCanBePrivate.Global

        #region MessageHelpingMethods

        public void ShowImportingMessage(CancellationTokenSource tokenSource, string fileName) {
			SetCurrentTokenSource(tokenSource);
            IsLoading = true;
            Message = "Parcing file:";
            SubMessage = fileName;
        }

        public void HideImportingMessage() {
            IsLoading = false;
        }

        public void ShowExcelConvertMessage(CancellationTokenSource tokenSource) {
			SetCurrentTokenSource(tokenSource);
            IsLoading = true;
            Message = "Export to Excel";
            SubMessage = "Running";
        }

		private void SetCurrentTokenSource(CancellationTokenSource tokenSource) {
			_currentCancellationTokenSource = tokenSource;
		}
        public void ShowBrowseLogFilesAndProcessMessage(CancellationTokenSource tokenSource, string dir) {
			SetCurrentTokenSource(tokenSource);
            IsLoading = true;
            Message = "Processing log files";
            SubMessage = "In: " + dir;
        }
        public void HideBrowseLogFilesAndProcessMessage() {
            IsLoading = false;
        }
        public void HideExcelConvertMessage(Action commandToRun = null) {
            if (commandToRun != null) {
                Message = "Export to Excel. Completed!";
                SubMessage = "Open exported file";
                _commandToRunAction = commandToRun;
                HaveCommandToRun = true;
            }
            else {
                IsLoading = false;
            }
        }
		public void HideMessage() {
			IsLoading = false;
		}
		public void ShowLoadMessage(CancellationTokenSource tokenSource, string fileName) {
			SetCurrentTokenSource(tokenSource);
			Message = "Loading from file:";
			SubMessage = fileName;
			IsLoading = true;
		}
		public void ShowSaveMessage(CancellationTokenSource tokenSource, string fileName) {
			SetCurrentTokenSource(tokenSource);
			Message = "Saving to file";
			SubMessage = fileName;
			IsLoading = true;
		}
		public void HideSaveMessage(Action commandToRun = null) {
			if (commandToRun != null) {
				Message = "Saving to file Completed!";
				SubMessage = "Open containing folder";
				_commandToRunAction = commandToRun;
				HaveCommandToRun = true;
			} else {
				IsLoading = false;
			}
		}

        #endregion
    }
}
