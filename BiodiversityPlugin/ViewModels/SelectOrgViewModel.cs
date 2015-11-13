using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BiodiversityPlugin.Models;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

namespace BiodiversityPlugin.ViewModels
{
    public class SelectOrgViewModel : ViewModelBase
    {
        private string _blibPath;
        private string _msgfPath;
        private string _orgName;
        private string _dbPath;
        private string _whichFunction;
        private bool _startEnable;
        private ObservableCollection<string> _filteredOrganisms;

        public string OrgName
        {
            get { return _orgName; }
            set
            {
                _orgName = value;
                RaisePropertyChanged();
                IsStartEnabled();
            }
        }

        public bool StartButtonEnabled
        {
            get { return _startEnable; }
            set
            {
                _startEnable = value;
                RaisePropertyChanged();
            }
        }

        private void IsStartEnabled()
        {
            if (!string.IsNullOrEmpty(OrgName)) 
            {
                StartButtonEnabled = true;
                RaisePropertyChanged();
            }
            else
            {
                StartButtonEnabled = false;
            }
        }

        public ObservableCollection<string> FilteredOrganisms
        {
            get { return _filteredOrganisms; }
            set { _filteredOrganisms = value; }
        }

        public RelayCommand UpdateExistingClassCommand { get; private set; }

        public SelectOrgViewModel(string dbpath, ObservableCollection<string> organisms, string blibPath, string msgfPath, string whichFunction)
        {
            _dbPath = dbpath;
            _filteredOrganisms = organisms;
            _blibPath = blibPath;
            _msgfPath = msgfPath;
            _whichFunction = whichFunction;
            UpdateExistingClassCommand = new RelayCommand(UpdateExistingClass);
        }

        private void UpdateExistingClass()
        {
            if (_whichFunction == "replace")
            {
                UpdateExistingOrganism.UpdateExisting(_orgName, _blibPath, _msgfPath, _dbPath);
            }
            else if (_whichFunction == "supplement")
            {
                SupplementOrgansim.Supplement(_orgName, _blibPath, _msgfPath, _dbPath);
            }
            
        }
    }
}
