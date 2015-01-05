using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using BiodiversityPlugin.Models;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;

namespace BiodiversityPlugin.ViewModels
{

    public class MainViewModel : ViewModelBase
    {
        public ObservableCollection<OrgPhylum> Organisms { get; private set; }
        public ObservableCollection<PathwayCatagory> Pathways { get; private set; }

        public Organism SelectedOrganism { get; private set; }
        public Pathway SelectedPathway { get; private set; }

        //This is for testing dynamic tab control
        public ObservableCollection<Pathway> SelectedPathways
        {
            get { return _selectedPathways; }
            private set
            {
                _selectedPathways = value;
                RaisePropertyChanged();
            }
        }

        public Visibility PathwayVisibility
        {
            get { return _visiblePathway; }
            private set
            {
                _visiblePathway = value;
                RaisePropertyChanged();
            }
        }

        public Uri PathwayImage
        {
            get { return _imageString; }
            private set
            {
                _imageString = value;
                PathwayVisibility = Visibility.Hidden;
                if (!string.IsNullOrEmpty(_imageString.OriginalString))
                {
                    PathwayVisibility = Visibility.Visible;
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
            get
            {
                return m_filteredProteins;
            }
            private set
            {
                m_filteredProteins = value;
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

        public RelayCommand NextTabCommand { get; private set; }
        public RelayCommand PreviousTabCommand { get; private set; }

        public int PathwayTabIndex
        {
            get { return m_pathwayTabIndex; }
            set { m_pathwayTabIndex = value; RaisePropertyChanged(); }
        }

        public int SelectedTabIndex
        {
            get { return _selectedTabIndex; }
            set
            {
                _selectedTabIndex = value;
                RaisePropertyChanged();
            }
        }

        public RelayCommand AcquireProteinsCommand { get; private set; }

        public RelayCommand ExportToSkylineCommand { get; private set; }

        public RelayCommand DisplayPathwayImagesCommand { get; private set; }

        public RelayCommand SelectAdditionalOrganismCommand { get; private set; }

        public RelayCommand OrganismReviewCommand { get; private set; }

        private readonly string _dbPath;

        public MainViewModel(IDataAccess orgData, IDataAccess pathData, string dbPath, string proteinsPath)
        {
            _proteins = PopulateProteins(proteinsPath);
            _dbPath = dbPath;
            Messenger.Default.Register<PropertyChangedMessage<bool>>(this, PathwaysSelectedChanged);
            Organisms = new ObservableCollection<OrgPhylum>(orgData.LoadOrganisms());
            Pathways = new ObservableCollection<PathwayCatagory>(pathData.LoadPathways());
            FilteredProteins = new ObservableCollection<ProteinInformation>();
            PreviousTabCommand = new RelayCommand(PreviousTab);
            NextTabCommand = new RelayCommand(NextTab);
            AcquireProteinsCommand = new RelayCommand(AcquireProteins);
            ExportToSkylineCommand = new RelayCommand(ExportToSkyline);
            DisplayPathwayImagesCommand = new RelayCommand(DisplayPathwayImages);
            SelectAdditionalOrganismCommand = new RelayCommand(SelectAdditionalOrganism);
            OrganismReviewCommand = new RelayCommand(OrganismReview);
            _selectedTabIndex = 0;
            _isOrganismSelected = false;
            _isPathwaySelected = false;
            _visibleProteins = Visibility.Hidden;
            _image = new ImageBrush();
            _imageString = new Uri(string.Format("..\\..\\..\\resources\\images\\map00010.png"), UriKind.Relative);
            _visiblePathway = Visibility.Hidden;
            _pathwaysSelected = 0;
            PathwayImage = _imageString;
            _selectedPathways = new ObservableCollection<Pathway>();
            SelectedPathways = _selectedPathways;
            ProteinsToExport = new List<ProteinInformation>();
        }

        public object SelectedOrganismTreeItem
        {
            get { return _selectedOrganismTreeItem; }
            set
            {
                _selectedOrganismTreeItem = value;
                SelectedOrganism = _selectedOrganismTreeItem as Organism;
                IsOrganismSelected = false;
                if (SelectedOrganism != null)
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

        private void PathwaysSelectedChanged(PropertyChangedMessage<bool> message)
        {
            if (message.PropertyName == "Selected" && message.Sender is Pathway)
            {
                if (message.NewValue == true)
                {
                    _pathwaysSelected++;
                    IsPathwaySelected = true;
                }
                else
                {
                    _pathwaysSelected--;
                    if (_pathwaysSelected == 0)
                    {
                        IsPathwaySelected = false;
                    }
                }
            }
        }

        private void PreviousTab()
        {
            if (SelectedTabIndex > 0)
                SelectedTabIndex--;
        }

        private void NextTab()
        {
            // Do nothing if no selected organism
            if (SelectedTabIndex == 0 && SelectedOrganism == null) return;
            // Do nothing if no selected pathway
            if (SelectedTabIndex == 1 && !IsPathwaySelected) return;
            SelectedTabIndex++;
        }

        private void OrganismReview()
        {
            SelectedTabIndex++;
            var org = SelectedOrganism;
            var orgPathList = new List<Tuple<Organism, Pathway>>();
            if (m_organismPathwayHistory != null)
            {
                orgPathList = m_organismPathwayHistory.ToList();
            }

            foreach (var pathway in SelectedPathways)
            {
                orgPathList.Add(new Tuple<Organism, Pathway>(org, pathway));
            }
            OrganismPathwayHistory = new ObservableCollection<Tuple<Organism, Pathway>>(orgPathList);
        }

        public ObservableCollection<Tuple<Organism, Pathway>> OrganismPathwayHistory
        {
            get { return m_organismPathwayHistory;}
            private set
            {
                m_organismPathwayHistory = value;
                RaisePropertyChanged();
            }
        }

        private void DisplayPathwayImages()
        {
            IsQuerying = true;

            var pwd = Directory.GetCurrentDirectory();
            var dataAccess = new DatabaseDataLoader(_dbPath);
            var pieces = pwd.Split('\\');
            var absPath = "";
            for (var i = 0; i < pieces.Count() - 3; i++)
            {
                absPath += string.Format("{0}{1}", pieces[i], '\\');
            }
            SelectedTabIndex++;
            var selectedPaths = new List<Pathway>();
            foreach (var catagory in Pathways)
            {
                foreach (var group in catagory.PathwayGroups)
                {
                    foreach (var pathway in group.Pathways)
                    {
                        if (pathway.Selected)
                        {
                            pathway.PathwayImage =
                                new Uri(string.Format("{0}resources\\images\\map{1}.png", absPath, pathway.KeggId),
                                    UriKind.Absolute);
                            pathway.ClearRectangles();
                            var koToCoordDict = new Dictionary<string, Tuple<int, int>>();
                            using (
                                var reader =
                                    new StreamReader(
                                        string.Format(string.Format("{0}resources\\coords\\path{1}KoCoords.txt",
                                            absPath,
                                            pathway.KeggId))))
                            {
                                var line = reader.ReadLine();
                                line = reader.ReadLine();
                                while (!string.IsNullOrEmpty(line))
                                {
                                    var linepieces = line.Split('\t');
                                    var coord = linepieces[2];
                                    var coordPieces = coord.Substring(1, coord.Length - 2).Split(',');
                                    if (!koToCoordDict.ContainsKey(linepieces[1]))
                                        koToCoordDict.Add(linepieces[1],
                                            new Tuple<int, int>(Convert.ToInt32(coordPieces[0]),
                                                Convert.ToInt32(coordPieces[1])));
                                    line = reader.ReadLine();
                                }
                            }
                            var koWithData = dataAccess.ExportKosWithData(pathway, SelectedOrganism);
                            var coordToName = new Dictionary<Tuple<int, int>, List<KeggKoInformation>>();
                            foreach (var ko in koWithData)
                            {
                                if (koToCoordDict.ContainsKey(ko.KeggKoId))
                                    if (!coordToName.ContainsKey(koToCoordDict[ko.KeggKoId]))
                                    {
                                        {
                                            coordToName[koToCoordDict[ko.KeggKoId]] = new List<KeggKoInformation>();
                                        }
                                        coordToName[koToCoordDict[ko.KeggKoId]].Add(ko);
                                    }
                            }
                            foreach (var coord in coordToName)
                            {
                                if (coord.Value.Count > 1)
                                {
                                    Console.WriteLine("hey, multiko");
                                }
                                pathway.AddRectangle(
                                    coord.Value, coord.Key.Item1,
                                    coord.Key.Item2);
                            }

                            selectedPaths.Add(pathway);

                            if (selectedPaths.Count == 1)
                            {
                                SelectedPathwayText = string.Format("Pathway: {0}", pathway.Name);
                            }
                            else if (selectedPaths.Count%4 == 0)
                            {
                                SelectedPathwayText += string.Format("\n\t{0}", pathway.Name);
                            }
                            else
                            {
                                SelectedPathwayText += string.Format(", {0}", pathway.Name);
                            }
                        }
                    }
                }
            }

            SelectedPathways = new ObservableCollection<Pathway>(selectedPaths);
            SelectedPathway = selectedPaths.First();
            PathwayTabIndex = 0;

        }

        private void AcquireProteins()
        {
            IsQuerying = true;

            var pwd = Directory.GetCurrentDirectory();
            var dataAccess = new DatabaseDataLoader(_dbPath);
            var pieces = pwd.Split('\\');
            var absPath = "";
            for (var i = 0; i < pieces.Count() - 3; i++)
            {
                absPath += string.Format("{0}{1}", pieces[i], '\\');
            }
            SelectedTabIndex++;
            var selectedPaths = SelectedPathways.ToList();

            //string[] queryingStrings =
            //{
            //    "Querying Database\nPlease Wait",
            //    "Querying Database.\nPlease Wait",
            //    "Querying Database..\nPlease Wait",
            //    "Querying Database...\nPlease Wait"
            //};

            //Task.Factory.StartNew(() =>
            //{
            //    int index = 0;
            //    while (IsQuerying)
            //    {
            //        Thread.Sleep(750);
            //        QueryString = queryingStrings[index%4];
            //        index++;
            //    }
            //});
            var accessions = new List<ProteinInformation>();
            if (SelectedPathway != null && SelectedOrganism != null)
            {
                accessions.AddRange(dataAccess.ExportAccessions(selectedPaths, SelectedOrganism));

                foreach (var accession in accessions)
                {
                    string proteinName;
                    if (_proteins.TryGetValue(accession.Accession, out proteinName))
                    {
                        accession.Name = proteinName;
                    }
                }
                IsPathwaySelected = true;
            }
            else
            {
                MessageBox.Show("Please select an organism and pathway.");
            }
            IsQuerying = false;

            if (FilteredProteins == null)
                FilteredProteins = new ObservableCollection<ProteinInformation>(accessions);
            else
            {
                foreach (var acc in accessions)
                {
                    if (!_protNames.Contains(acc.Accession))
                    {
                        _protNames.Add(acc.Accession);
                        FilteredProteins.Add(acc);
                        NumProteinsText = string.Format("Proteins ({0})", FilteredProteins.Count);
                        VisibleProteins = FilteredProteins.Count > 0 ? Visibility.Visible : Visibility.Hidden;
                    }
                }
            }
        }

        private void SelectAdditionalOrganism()
        {
            foreach (var protein in FilteredProteins)
            {
                if (!ProteinsToExport.Contains(protein))
                {
                    ProteinsToExport.Add(protein);
                }
            }
            SelectedTabIndex = 0;
            SelectedOrganism = null;
            FilteredProteins.Clear();

            foreach (var pathwayCatagory in Pathways)
            {
                foreach (var pathwayGroup in pathwayCatagory.PathwayGroups)
                {
                    foreach (var pathway in pathwayGroup.Pathways)
                    {
                        if (pathway.PathwayImage != null)
                        {
                            pathway.PathwayCanvas.Children.Clear();
                            pathway.PathwayImage = null;
                        }
                    }
                }
            }
        }

        private void ExportToSkyline()
        {
            foreach (var protein in FilteredProteins)
            {
                if (!ProteinsToExport.Contains(protein))
                {
                    ProteinsToExport.Add(protein);
                }
            }
            foreach (var protein in ProteinsToExport)
            {
                Console.WriteLine(string.Format("{0}: {1} - {2}", protein.Accession, protein.Name, protein.Description));
            }
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
        private bool _isQuerying;
        private string _queryString;
        private int _selectedTabIndex;
        private ImageBrush _image;
        private Visibility _visiblePathway;
        private int _pathwaysSelected;
        private Uri _imageString;
        private ObservableCollection<Pathway> _selectedPathways;
        private List<ProteinInformation> m_proteinsToExport;
        private List<string> _protNames = new List<string>();
        private ObservableCollection<Tuple<Organism, Pathway>> m_organismPathwayHistory;
        private int m_pathwayTabIndex;

        public List<ProteinInformation> ProteinsToExport
        {
            get { return m_proteinsToExport;}
            private set
            {
                m_proteinsToExport = value;
                RaisePropertyChanged("ProteinsToExport");
            }
        }

        public void AddToExport(ProteinInformation proteinToAdd)
        {
            ProteinsToExport.Add(proteinToAdd);
        }

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
