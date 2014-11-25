using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
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

        public RelayCommand ExportToSkylineCommand { get; private set; }

        public MainViewModel(IDataAccess orgData, IDataAccess pathData, string organismPath, string pathwaysPath)
        {
            Organisms = new ObservableCollection<OrgPhylum>(orgData.LoadOrganisms(organismPath));
            Pathways = new ObservableCollection<PathwayCatagory>(pathData.LoadPathways(pathwaysPath));
            ExportToSkylineCommand = new RelayCommand(ExportToSkyline);
        }

        public object SelectedOrganismTreeItem
        {
            get { return _selectedOrganismTreeItem; }
            set
            {
                _selectedOrganismTreeItem = value;
                SelectedOrganism = _selectedOrganismTreeItem as Organism;
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
                RaisePropertyChanged();
            }
        }

        private void ExportToSkyline()
        {
            if (SelectedPathway != null && SelectedOrganism != null)
            {
                var dataAccess = new DatabaseDataLoader();
                var accessions = dataAccess.ExportAccessions(SelectedPathway, SelectedOrganism);
                int numInLine = 0;
                string acc = "";
                foreach (var line in accessions)
                {
                    acc += line;
                    if (numInLine%7 != 6)
                    {
                        acc += ", ";
                    }
                    else
                    {
                        acc += "\n";
                    }
                    numInLine++;
                }

                //string acc = accessions.Aggregate("", (current, accession) => current + ("\n" + accession));
                MessageBox.Show(acc);   
            }
            else
            {
                MessageBox.Show("Please select an organism and pathway.");
            }
        }

        private object _selectedOrganismTreeItem;
        private object _selectedPathwayTreeItem;
    }
}
