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

        public SelectOrgViewModel(string dbpath, ObservableCollection<string> organisms, string blibPath, string msgfPath)
        {
            _dbPath = dbpath;
            _filteredOrganisms = organisms;
            _blibPath = blibPath;
            _msgfPath = msgfPath;
            UpdateExistingClassCommand = new RelayCommand(UpdateExistingClass);
        }

        private void UpdateExistingClass()
        {
            UpdateExistingOrganism.UpdateExisting(_orgName, _blibPath, _msgfPath, _dbPath);
        }
    }
}
