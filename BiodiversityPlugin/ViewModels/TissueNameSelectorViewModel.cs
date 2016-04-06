using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

namespace BiodiversityPlugin.ViewModels
{
    public class TissueNameSelectorViewModel : ViewModelBase
    {
        private string _tissueName;
        private string _tissueTaxon;
        private string _tissueKeggCode;

        public TissueNameSelectorViewModel()
        {
            TissueName = "";
            TissueTaxon = "";
            TissueKeggCode = "";
            CancelCommand = new RelayCommand(Cancel);
            AcceptCommand = new RelayCommand(ApplyInformation);
        }
        public Action CloseAction { get; set; }
        public RelayCommand CancelCommand { get; private set; }
        public RelayCommand AcceptCommand { get; private set; }

        public string TissueName
        {
            get { return _tissueName; }
            set
            {
                _tissueName = value;
                RaisePropertyChanged();
            }
        }

        public string TissueTaxon
        {
            get { return _tissueTaxon; }
            set
            {
                _tissueTaxon = value;
                RaisePropertyChanged();
            }
        }

        public string TissueKeggCode
        {
            get { return _tissueKeggCode; }
            set
            {
                _tissueKeggCode = value;
                RaisePropertyChanged();
            }
        }

        private void Cancel()
        {
            this.CloseAction();
        }

        public string InputText { get; set; }

        private void ApplyInformation()
        {
            TissueName = "Homo sapiens " + InputText;
            var addendum = InputText.Replace(' ', '_');
            TissueKeggCode = "hsa_" + addendum;
            TissueTaxon = "9606." + addendum;
            CloseAction();
        }

    }
}
