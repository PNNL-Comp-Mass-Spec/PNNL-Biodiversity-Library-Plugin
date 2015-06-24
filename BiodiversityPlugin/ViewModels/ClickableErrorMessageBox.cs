using System.Diagnostics;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

namespace BiodiversityPlugin.ViewModels
{
    public class ClickableErrorMessageBox :ViewModelBase
    {
        private string _errorLink;
        private string _errorLogLocation;

        public RelayCommand OpenFolderCommand { get; private set; }

        public ClickableErrorMessageBox(string errorLocation)
        {
            ErrorLogLocation = errorLocation;
            OpenFolderCommand = new RelayCommand(OpenFolder);
        }

        public string ErrorLogLocation
        {
            get
            {
                return _errorLogLocation;;
            }
            set
            {
                _errorLogLocation = value;
                RaisePropertyChanged();
            }
        }

        public string ErrorLink
        {
            get
            {
                return _errorLink;
            }
            set
            {
                _errorLink = value;
                RaisePropertyChanged();
            }
        }

        private void OpenFolder()
        {
            Process.Start(ErrorLogLocation);
        }

    }
}
