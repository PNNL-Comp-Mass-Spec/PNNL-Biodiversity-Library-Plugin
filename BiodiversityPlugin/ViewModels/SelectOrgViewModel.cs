using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using BiodiversityPlugin.Models;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Window = MathNet.Numerics.Window;

namespace BiodiversityPlugin.ViewModels
{
    public class SelectOrgViewModel : ViewModelBase
    {
        private string _blibPath;
        private List<string> _msgfPath;
        private OrganismWithFlag _orgName;
        private string _dbPath;
        private string _whichFunction;
        private bool _startEnable;
        private ObservableCollection<OrganismWithFlag> _filteredOrganisms;

        public OrganismWithFlag OrgName
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
            if (!string.IsNullOrEmpty(OrgName.OrganismName)) 
            {
                StartButtonEnabled = true;
                RaisePropertyChanged();
            }
            else
            {
                StartButtonEnabled = false;
            }
        }

        public ObservableCollection<OrganismWithFlag> FilteredOrganisms
        {
            get { return _filteredOrganisms; }
            set { _filteredOrganisms = value; }
        }

        public RelayCommand UpdateExistingClassCommand { get; private set; }
        //public RelayCommand<IClosable> CloseWindowCommand { get; private set; }

        public SelectOrgViewModel(string dbpath, ObservableCollection<OrganismWithFlag> organisms, string blibPath, List<string> msgfPath, string whichFunction)
        {
            _dbPath = dbpath;
            _filteredOrganisms = organisms;
            _blibPath = blibPath;
            _msgfPath = msgfPath;
            _whichFunction = whichFunction;
            UpdateExistingClassCommand = new RelayCommand(UpdateExistingClass);
            //this.CloseWindowCommand = new RelayCommand<IClosable>(this.CloseWindow);
        }

        private void UpdateExistingClass()
        {
            if (_whichFunction == "replace")
            {
               UpdateExistingOrganism.UpdateExisting(_orgName.OrganismName, _blibPath, _msgfPath, _dbPath);
                var windowArray = new System.Windows.Window[3];
               System.Windows.Application.Current.Windows.CopyTo(windowArray, 0);
                foreach (var window in windowArray)
                {
                    //Close all but the main window
                    if (window.Title != "PNNL Biodiversity Library")
                    {
                        window.Close();
                    }                  
                }
            }
            else if (_whichFunction == "supplement")
            {
                SupplementOrgansim.Supplement(_orgName.OrganismName, _blibPath, _msgfPath, _dbPath);
                var windowArray = new System.Windows.Window[3];
                System.Windows.Application.Current.Windows.CopyTo(windowArray, 0);
                foreach (var window in windowArray)
                {
                    //Close all but the main window
                    if (window.Title != "PNNL Biodiversity Library")
                    {
                        window.Close();
                    }
                }
            }
            
        }
    }
}
