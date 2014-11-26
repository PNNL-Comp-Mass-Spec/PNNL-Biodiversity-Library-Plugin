using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using BiodiversityPlugin.Models;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

namespace BiodiversityPlugin.ViewModels
{

    public class MainViewModel: ViewModelBase
    {
        public ObservableCollection<OrgPhylum> Organisms { get; private set; }
        public ObservableCollection<PathwayCatagory> Pathways { get; private set; } 

        public Organism SelectedOrganism { get; private set; }
        public Pathway SelectedPathway { get; private set; }

        public ObservableCollection<ProteinInformation> FilteredProteins
        {
            get { return m_filteredProteins; }
            private set
            {
                m_filteredProteins = value;
                RaisePropertyChanged();
            }
        } 

        public string SelectedOrganismText
        {
            get { return m_selectedOrganismText; }
            private set
            {
                m_selectedOrganismText = value;
                IsOrganismSelected = true;
                RaisePropertyChanged();
            }
        }

        public string SelectedPathwayText
        {
            get { return m_selectedPathwayText; }
            private set
            {
                m_selectedPathwayText = value;
                IsPathwaySelected = true;
                RaisePropertyChanged();
            }
        }

        public bool IsOrganismSelected
        {
            get { return _isOrganismSelected; }
            set
            {
                _isOrganismSelected = value;
                RaisePropertyChanged();
            }
        }

        public bool IsPathwaySelected
        {
            get { return _isPathwaySelected; }
            set
            {
                _isPathwaySelected = value;
                RaisePropertyChanged();
            }
        }

        public RelayCommand ExportToSkylineCommand { get; private set; }

        private readonly string _dbPath;

        public MainViewModel(IDataAccess orgData, IDataAccess pathData, string dbPath)
        {
            _dbPath = dbPath;
            Organisms = new ObservableCollection<OrgPhylum>(orgData.LoadOrganisms());
            Pathways = new ObservableCollection<PathwayCatagory>(pathData.LoadPathways());
            FilteredProteins = new ObservableCollection<ProteinInformation>();
            ExportToSkylineCommand = new RelayCommand(ExportToSkyline);
            _isOrganismSelected = false;
            _isPathwaySelected = false;
        }

        public object SelectedOrganismTreeItem
        {
            get { return _selectedOrganismTreeItem; }
            set
            {
                _selectedOrganismTreeItem = value;
                SelectedOrganism = _selectedOrganismTreeItem as Organism;
                IsOrganismSelected = false;
                if(SelectedOrganism != null)
                    SelectedOrganismText = string.Format("Organism: {0}", SelectedOrganism.Name);
                RaisePropertyChanged();
            }
        }

        public object SelectedPathwayTreeItem
        {
            get { return _selectedPathwayTreeItem; }
            set
            {
                _selectedPathwayTreeItem = value;
                SelectedPathway = _selectedPathwayTreeItem as Pathway;
                IsPathwaySelected = false;
                if (SelectedPathway != null)
                    SelectedPathwayText = string.Format("Pathway: {0}", SelectedPathway.Name);
                RaisePropertyChanged();
            }
        }

        private void ExportToSkyline()
        {
            if (SelectedPathway != null && SelectedOrganism != null)
            {
                var dataAccess = new DatabaseDataLoader(_dbPath);
                var accessions = dataAccess.ExportAccessions(SelectedPathway, SelectedOrganism);
                FilteredProteins = new ObservableCollection<ProteinInformation>(accessions);
                IsPathwaySelected = true;
            }
            else
            {
                MessageBox.Show("Please select an organism and pathway.");
            }
        }

        private object _selectedOrganismTreeItem;
        private object _selectedPathwayTreeItem;

        private string m_selectedOrganismText;
        private string m_selectedPathwayText;
        private ObservableCollection<ProteinInformation> m_filteredProteins;
        private bool _isPathwaySelected;
        private bool _isOrganismSelected;
    }
}
