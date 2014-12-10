using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Entity.Core.Metadata.Edm;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading;
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

        public Visibility UrlVisible
        {
            get { return _visibleUrl; }
            private set
            {
                _visibleUrl = value;
                RaisePropertyChanged();
            }
        }

        public System.Uri MapUrl
        {
            get { return _url; }
            private set
            {
                _url = value;
                UrlVisible = Visibility.Hidden;
                if (!string.IsNullOrWhiteSpace(_url.AbsolutePath))
                {
                    UrlVisible = Visibility.Visible;
                }
                RaisePropertyChanged();
            }
        }

        public Visibility VisibleProteins
        {
            get { return _visibleProteins; }
            private set
            {
                _visibleProteins = value;
                RaisePropertyChanged();
            }
        }

        public ObservableCollection<ProteinInformation> FilteredProteins
        {
            get { return m_filteredProteins; }
            private set
            {
                m_filteredProteins = value;
                NumProteinsText = string.Format("Proteins ({0})", value.Count);
                VisibleProteins = value.Count > 0 ? Visibility.Visible : Visibility.Hidden;
                RaisePropertyChanged();
            }
        }

        public string NumProteinsText
        {
            get { return m_numberProteinsText; }
            private set
            {
                m_numberProteinsText = value;
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

        public MainViewModel(IDataAccess orgData, IDataAccess pathData, string dbPath, string proteinsPath)
        {
            _proteins = PopulateProteins(proteinsPath);
            _dbPath = dbPath;
            Organisms = new ObservableCollection<OrgPhylum>(orgData.LoadOrganisms());
            Pathways = new ObservableCollection<PathwayCatagory>(pathData.LoadPathways());
            FilteredProteins = new ObservableCollection<ProteinInformation>();
            ExportToSkylineCommand = new RelayCommand(ExportToSkyline);
            _isOrganismSelected = false;
            _isPathwaySelected = false;
            _visibleProteins = Visibility.Hidden;
            _url = new Uri("http://www.google.com");
            _visibleUrl = Visibility.Hidden;
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
            IsQuerying = true;

            string[] queryingStrings =
			    {
				    "Querying Database\nPlease Wait",
				    "Querying Database.\nPlease Wait",
				    "Querying Database..\nPlease Wait",
				    "Querying Database...\nPlease Wait"
			    };

            Task.Factory.StartNew(() =>
            {
                int index = 0;
                while (IsQuerying)
                {
                    Thread.Sleep(750);
                    QueryString = queryingStrings[index % 4];
                    index++;
                }
            });
            Task.Factory.StartNew(() =>
            {
                if (SelectedPathway != null && SelectedOrganism != null)
                {

                    var dataAccess = new DatabaseDataLoader(_dbPath);
                    var accessions = dataAccess.ExportAccessions(SelectedPathway, SelectedOrganism);
                    if (SelectedOrganism.OrgCode != "")
                    {
                        MapUrl = new Uri(string.Format("http://www.genome.jp/kegg-bin/show_pathway?org_name={0}&mapno={1}",
                            SelectedOrganism.OrgCode, SelectedPathway.KeggId));
                    }
                    foreach (var accession in accessions)
                    {
                        string proteinName;
                        if (_proteins.TryGetValue(accession.Accession, out proteinName))
                        {
                            accession.Name = proteinName;
                        }
                    }
                    FilteredProteins = new ObservableCollection<ProteinInformation>(accessions);
                    IsPathwaySelected = true;
                }
                else
                {
                    MessageBox.Show("Please select an organism and pathway.");
                }
                IsQuerying = false;
            });
        }

        private Dictionary<string, string> PopulateProteins(string fileName)
        {
            var file = File.ReadAllLines(fileName);
            int lineIndex = 0;
            var proteins = new Dictionary<string, string>();
            foreach (var line in file)
            {
                if (lineIndex++ == 0) continue;
                var parts = line.Split('\t');
                if (parts.Length < 3) continue;
                if (!proteins.ContainsKey(parts[0]))
                {
                    proteins.Add(parts[0], parts[2]);
                }
            }
            return proteins;
        }

        private object _selectedOrganismTreeItem;
        private object _selectedPathwayTreeItem;

        private string m_selectedOrganismText;
        private string m_selectedPathwayText;
        private string m_numberProteinsText;
        private ObservableCollection<ProteinInformation> m_filteredProteins;
        private bool _isPathwaySelected;
        private bool _isOrganismSelected;
        private readonly Dictionary<string, string> _proteins;
        private Visibility _visibleProteins;
        private  bool _isQuerying;
        private string _queryString;
        private System.Uri _url;
        private Visibility _visibleUrl;

        public bool IsQuerying
        {
            get { return _isQuerying; }
            private set
            {
                _isQuerying = value;
                RaisePropertyChanged();
            }
        }

        public string QueryString
        {
            get { return _queryString; }
            private set
            {
                _queryString = value;
                RaisePropertyChanged();
            }
        }
    }
}
