using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using BiodiversityPlugin.Models;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using KeggDataLibrary.DataManagement;

namespace BiodiversityPlugin.ViewModels
{
    public class TypeOrgViewModel : ViewModelBase
    {
        private string _organismName;
        private bool _startEnable;
        private string _dbPath;
        private string _blibPath;
        private string _selectedValue;
        private List<string> _msgfPath;
        private List<string> _allKeggOrgs;
        private ObservableCollection<string> _filteredOrganisms;        
        private Visibility _filterVisibility;
        private Visibility _loadingVisible;

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
            if (!string.IsNullOrEmpty(OrganismName)) //check to make sure at least the organism name is there
            {
                StartButtonEnabled = true;               
                RaisePropertyChanged();
            }
            else
            {
                StartButtonEnabled = false;
            }
        }

        public string OrganismName
        {
            get { return _organismName; }
            set
            {
                _organismName = value;
                RaisePropertyChanged();
                IsStartEnabled();
            }
        }

        public List<string> AllKeggOrgs
        {
            get { return _allKeggOrgs; }
            set { _allKeggOrgs = value; }
        } 

        public ObservableCollection<String> FilteredOrganisms
        {
            get { return _filteredOrganisms; }
            set
            {
                _filteredOrganisms = value;
                RaisePropertyChanged();
            }
        }

        public Visibility FilterBoxVisible
        {
            get { return _filterVisibility; }
            set
            {
                _filterVisibility = value;
                RaisePropertyChanged();
            }
        }

        public string SelectedValue
        {
            get { return _selectedValue; }
            set
            {
                _selectedValue = value;
                RaisePropertyChanged();
                
                var filtered = new List<string>();
                if (AllKeggOrgs != null)
                {
                    foreach (var org in _allKeggOrgs)
                    {
                        if (org.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            filtered.Add(org);
                        }
                    }
                }
                filtered.Sort();
                FilteredOrganisms = new ObservableCollection<string>(filtered);
                FilterBoxVisible = Visibility.Hidden;
                if (FilteredOrganisms.Count > 0)
                {
                    FilterBoxVisible = Visibility.Visible;
                }
            }
        }

        public Action CloseAction { get; set; }

        private void Close()
        {
            this.CloseAction();
        }

        public RelayCommand InsertNewOrganismCommand { get; set; }
        public RelayCommand ClearFilterCommand { get; private set; }

        public TypeOrgViewModel(string dbpath, string blibPath, List<string> msgfPath)
        {           
            _dbPath = dbpath;
            _blibPath = blibPath;
            _msgfPath = msgfPath;
            SelectedValue = "";
            AllKeggOrgs = InsertNewOrganism.GetListOfKeggOrganisms();
            InsertNewOrganismCommand = new RelayCommand(InsertNewOrg);
            ClearFilterCommand = new RelayCommand(ClearFilter);
        }

        private void InsertNewOrg()
        {            
            InsertNewOrganism.InsertNew(_organismName, _blibPath, _msgfPath, _dbPath);
            Close();
        }

        private void ClearFilter()
        {
            SelectedValue = "";
        }
    }
}
