﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Media;
using BiodiversityPlugin.DataManagement;
using BiodiversityPlugin.Models;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;

namespace BiodiversityPlugin.ViewModels
{

    public class MainViewModel : ViewModelBase
    {
        #region Private attributes
        
        private readonly string _dbPath;
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
        private string m_overviewText;
        private List<string> m_organismList;
        private string m_selectedValue;
        private ObservableCollection<OrganismPathwayProteinAssociation> m_pathwayProteinAssociation;
        private Visibility m_filterVisibility;
        private ObservableCollection<string> m_filteredOrganisms;
        private ObservableCollection<string> m_listPathways;
        private string m_listPathwaySelectedItem;
        private bool m_listPathwaySelected;
        private bool m_pathwaysTabEnabled;
        private bool m_selectionTabEnabled;
        private bool m_reviewTabEnabled;

        #endregion

        #region Public Properties

        public ObservableCollection<OrgPhylum> Organisms { get; private set; }
        public ObservableCollection<PathwayCatagory> Pathways { get; private set; }

        public Organism SelectedOrganism { get; private set; }
        public Pathway SelectedPathway { get; private set; }

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
                if (value != null && !string.IsNullOrEmpty(_imageString.OriginalString))
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
                {
                    SelectedPathwayText = string.Format("Pathway: {0}", SelectedPathway.Name);
                    SelectedPathway.Selected = true;
                }
                RaisePropertyChanged();
            }
        }

        public ObservableCollection<Tuple<Organism, Pathway>> OrganismPathwayHistory
        {
            get { return m_organismPathwayHistory; }
            private set
            {
                m_organismPathwayHistory = value;
                RaisePropertyChanged();
            }
        }

        public List<ProteinInformation> ProteinsToExport
        {
            get { return m_proteinsToExport; }
            private set
            {
                m_proteinsToExport = value;
                RaisePropertyChanged("ProteinsToExport");
            }
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

        public string OverviewText
        {
            get { return m_overviewText; }
            set
            {
                m_overviewText = value;
                RaisePropertyChanged();
            }
        }

        public List<string> OrganismList
        {
            get { return m_organismList; }
            set
            {
                m_organismList = value;
                RaisePropertyChanged();
            }
        }

        public string SelectedValue
        {
            get { return m_selectedValue; }
            set
            {
                m_selectedValue = value;
                RaisePropertyChanged();
                var filtered = new List<string>();
                foreach (var phylum in Organisms)
                {
                    foreach (var orgClass in phylum.OrgClasses)
                    {
                        foreach (var organism in orgClass.Organisms)
                        {
                            if (organism.Name.ToUpper().StartsWith(value.ToUpper()))
                            {
                                filtered.Add(organism.Name);
                            }
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
                SelectedOrganismTreeItem = null;
            }
        }

        public ObservableCollection<OrganismPathwayProteinAssociation> PathwayProteinAssociation
        {
            get { return m_pathwayProteinAssociation; }
            set
            {
                m_pathwayProteinAssociation = value;
                RaisePropertyChanged("PathwayProteinAssociation");
            }
        }


        public Visibility FilterBoxVisible
        {
            get { return m_filterVisibility; }
            set
            {
                m_filterVisibility = value;
                RaisePropertyChanged();
            }
        }

        public ObservableCollection<String> FilteredOrganisms
        {
            get { return m_filteredOrganisms; }
            set
            {
                m_filteredOrganisms = value;
                RaisePropertyChanged();
            }
        }

        public string SelectedListOrg
        {
            get { return "Selected"; }
            set
            {
                foreach (var phylum in Organisms)
                {
                    foreach (var orgClass in phylum.OrgClasses)
                    {
                        foreach (var organism in orgClass.Organisms)
                        {
                            if (organism.Name == value)
                            {
                                SelectedOrganismTreeItem = organism;
                                SelectedTabIndex = SelectedTabIndex;
                                return;
                            }
                        }
                    }
                }
            }
        }

        public ObservableCollection<string> ListPathways
        {
            get { return m_listPathways; }
            set
            {
                m_listPathways = value;
                SelectedTabIndex = SelectedTabIndex;
                RaisePropertyChanged();
            }
        }

        public string ListPathwaySelectedItem
        {
            get { return m_listPathwaySelectedItem; }
            set
            {
                m_listPathwaySelectedItem = value;
                ListPathwaySelected = false;
                if (ListPathways.Contains(value))
                {
                    ListPathwaySelected = true;
                }
                RaisePropertyChanged();
            }
        }

        public bool ListPathwaySelected
        {
            get { return m_listPathwaySelected; }
            set
            {
                m_listPathwaySelected = value;
                RaisePropertyChanged();
            }
        }

        public bool PathwaysTabEnabled
        {
            get { return m_pathwaysTabEnabled; }
            set
            {
                m_pathwaysTabEnabled = value;
                RaisePropertyChanged();
            }
        }

        public bool SelectionTabEnabled
        {
            get { return m_selectionTabEnabled; }
            set
            {
                m_selectionTabEnabled = value;
                RaisePropertyChanged();
            }
        }

        public bool ReviewTabEnabled
        {
            get { return m_reviewTabEnabled; }
            set
            {
                m_reviewTabEnabled = value;
                RaisePropertyChanged();
            }
        }
        #endregion

        #region Commands

        public RelayCommand NextTabCommand { get; private set; }
        public RelayCommand PreviousTabCommand { get; private set; }
        public RelayCommand AcquireProteinsCommand { get; private set; }
        public RelayCommand ExportToSkylineCommand { get; private set; }
        public RelayCommand DisplayPathwayImagesCommand { get; private set; }
        public RelayCommand SelectAdditionalOrganismCommand { get; private set; }
        public RelayCommand OrganismReviewCommand { get; private set; }
        public RelayCommand DeleteSelectedPathwayCommand { get; set; }

        #endregion

        #region TabIndexes

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
                PathwaysTabEnabled = false;
                SelectionTabEnabled = false;
                ReviewTabEnabled = false;
                RaisePropertyChanged();
                if (SelectedTabIndex > 2 || SelectedOrganism != null)
                {
                    PathwaysTabEnabled = true;
                }
                if (SelectedTabIndex > 3 || ListPathways.Count != 0)
                {
                    SelectionTabEnabled = true;
                }
                if (SelectedTabIndex == 3)
                {
                    ReviewTabEnabled = true;
                }
            }
        }

        #endregion
        
        public MainViewModel(IDataAccess orgData, IDataAccess pathData, string dbPath, string proteinsPath)
        {
            _proteins = PopulateProteins(proteinsPath);
            _dbPath = dbPath;
            
            Messenger.Default.Register<PropertyChangedMessage<bool>>(this, PathwaysSelectedChanged);
            var organismList = new List<string>();
            var organisms = orgData.LoadOrganisms(ref organismList);
            
            organismList.Sort();
            OrganismList = organismList;
            organisms.Sort((x, y) => x.PhylumName.CompareTo(y.PhylumName));
            Organisms = new ObservableCollection<OrgPhylum>(organisms);
            Pathways = new ObservableCollection<PathwayCatagory>(pathData.LoadPathways());
            
            FilteredProteins = new ObservableCollection<ProteinInformation>();
            PreviousTabCommand = new RelayCommand(PreviousTab);
            NextTabCommand = new RelayCommand(NextTab);
            AcquireProteinsCommand = new RelayCommand(AcquireProteins);
            ExportToSkylineCommand = new RelayCommand(ExportToSkyline);
            DisplayPathwayImagesCommand = new RelayCommand(DisplayPathwayImages);
            SelectAdditionalOrganismCommand = new RelayCommand(SelectAdditionalOrganism);
            OrganismReviewCommand = new RelayCommand(OrganismReview);
            DeleteSelectedPathwayCommand = new RelayCommand(DeleteSelectedPathway);
            
            _selectedTabIndex = 0;
            _isOrganismSelected = false;
            _isPathwaySelected = false;
            _visibleProteins = Visibility.Hidden;
            
            _image = new ImageBrush();
            _imageString = null;
            _visiblePathway = Visibility.Hidden;
           
            _pathwaysSelected = 0;
            ListPathways = new ObservableCollection<string>();
            PathwayImage = _imageString;
            
            _selectedPathways = new ObservableCollection<Pathway>();
            SelectedPathways = _selectedPathways;
            
            ProteinsToExport = new List<ProteinInformation>();
            PathwayProteinAssociation = new ObservableCollection<OrganismPathwayProteinAssociation>();
            SelectedValue = "";
        }

        private void DeleteSelectedPathway()
        {
            foreach (var pathwayCatagory in Pathways)
            {
                foreach (var pathwayGroup in pathwayCatagory.PathwayGroups)
                {
                    foreach (var pathway in pathwayGroup.Pathways)
                    {
                        if (ListPathwaySelectedItem == pathway.Name)
                        {
                            pathway.Selected = false;
                            var temp = ListPathways;
                            temp.Remove(ListPathwaySelectedItem);
                            ListPathways = temp;
                        }
                    }
                }
            }
        }
        
        private void PathwaysSelectedChanged(PropertyChangedMessage<bool> message)
        {
            if (message.PropertyName == "Selected" && message.Sender is Pathway)
            {
                var old = ListPathways;
                var sender = message.Sender as Pathway;
                if (message.NewValue == true)
                {
                    if (!old.Contains(sender.Name))
                    {
                        old.Add(sender.Name);
                    }
                    ListPathways = old;
                    _pathwaysSelected++;
                    IsPathwaySelected = true;
                }
                else
                {
                    old.Remove(sender.Name);
                    ListPathways = old;
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
            {
                SelectedTabIndex--;
            }
        }

        private void NextTab()
        {
            // Do nothing if no selected organism
            if (SelectedTabIndex == 1 && SelectedOrganism == null) return;
            // Do nothing if no selected pathway
            if (SelectedTabIndex == 2 && !IsPathwaySelected) return;
            SelectedTabIndex++;
        }

        private void OrganismReview()
        {
            SelectedTabIndex = 4;
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
        
        private void DisplayPathwayImages()
        {
            IsQuerying = true;

            var pwd = System.Reflection.Assembly.GetExecutingAssembly().CodeBase;
            var dir = System.IO.Path.GetDirectoryName(pwd);
            
            var dataAccess = new DatabaseDataLoader(_dbPath);
            var pieces = pwd.Split('\\');
            var absPath = dir.Substring(6);
            SelectedTabIndex = 3;
            var selectedPaths = new List<Pathway>();
            foreach (var catagory in Pathways)
            {
                foreach (var group in catagory.PathwayGroups)
                {
                    foreach (var pathway in group.Pathways)
                    {
                        if (pathway.Selected)
                        {
                            pathway.PathwayImage = new Uri(string.Format("http://rest.kegg.jp/get/map{0}/image", pathway.KeggId));
                            pathway.ClearRectangles();
                            if (File.Exists(string.Format(string.Format("{0}\\DataFiles\\coords\\path{1}KoCoords.txt",
                                absPath,
                                pathway.KeggId))))
                            {
                                var koToCoordDict = new Dictionary<string, List<Tuple<int, int>>>();
                                using (
                                    var reader =
                                        new StreamReader(
                                            string.Format(string.Format("{0}\\DataFiles\\coords\\path{1}KoCoords.txt",
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
                                            koToCoordDict.Add(linepieces[1], new List<Tuple<int, int>>());
                                        koToCoordDict[linepieces[1]].Add(new Tuple<int, int>(Convert.ToInt32(coordPieces[0]),
                                                    Convert.ToInt32(coordPieces[1])));
                                        line = reader.ReadLine();
                                    }
                                }
                                var koWithData = dataAccess.ExportKosWithData(pathway, SelectedOrganism);
                                var coordToName = new Dictionary<Tuple<int, int>, List<KeggKoInformation>>();
                                foreach (var ko in koWithData)
                                {
                                    if (koToCoordDict.ContainsKey(ko.KeggKoId))
                                    {
                                        foreach (var coord in koToCoordDict[ko.KeggKoId])
                                        {
                                            if (!coordToName.ContainsKey(coord))
                                            {

                                                coordToName[coord] = new List<KeggKoInformation>();
                                            }
                                            coordToName[coord].Add(ko);
                                        }
                                    }
                                }
                                foreach (var coord in coordToName)
                                {
                                    pathway.AddDataRectangle(
                                        coord.Value, coord.Key.Item1,
                                        coord.Key.Item2, Colors.Red);
                                }

                                var koWithoutData = dataAccess.ExportKosWithoutData(pathway, SelectedOrganism);
                                var coordsToName = new Dictionary<Tuple<int, int>, List<KeggKoInformation>>();
                                foreach (var ko in koWithoutData)
                                {
                                    if (koToCoordDict.ContainsKey(ko.KeggKoId))
                                    {
                                        foreach (var coord in koToCoordDict[ko.KeggKoId])
                                        {
                                            if (!coordToName.ContainsKey(coord))
                                            {
                                                if (!coordsToName.ContainsKey(coord))
                                                {

                                                    coordsToName[coord] = new List<KeggKoInformation>();
                                                }
                                                coordsToName[coord].Add(ko);
                                            }
                                        }
                                    }
                                }
                                foreach (var coord in coordsToName)
                                {
                                    pathway.AddRectangle(
                                        coord.Value, coord.Key.Item1,
                                        coord.Key.Item2, Colors.Blue);
                                }
                                var legend = new KeggKoInformation("legend", "legend", "legend");
                                var legendList = new List<KeggKoInformation>();
                                legendList.Add(legend);
                                pathway.AddRectangle(legendList, 10,
                                    5, Colors.Red);
                                pathway.WriteFoundText(10, 5, SelectedOrganism.Name);
                                
                                pathway.AddRectangle(
                                    legendList, 10,
                                    22, Colors.Blue);
                                pathway.WriteNotfoundText(10, 22, SelectedOrganism.Name);

                            }
                            selectedPaths.Add(pathway);

                            if (selectedPaths.Count == 1)
                            {
                                SelectedPathwayText = string.Format("Pathway: {0}", pathway.Name);
                            }
                            else if (selectedPaths.Count%3 == 0)
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
            SelectedTabIndex = 4;
            var selectedPaths = SelectedPathways.ToList();
            var accessions = new List<ProteinInformation>();
            if (SelectedPathway != null && SelectedOrganism != null)
            {
                foreach (var pathway in selectedPaths)
                {
                    var temp = new List<Pathway> {pathway};
                    var pathwayAcc = dataAccess.ExportAccessions(temp, SelectedOrganism);
                    accessions.AddRange(pathwayAcc);

                    var association = new OrganismPathwayProteinAssociation();
                    association.Pathway = pathway.Name;
                    association.Organism = SelectedOrganism.Name;
                    association.GeneList = new ObservableCollection<ProteinInformation>();
                    foreach (var acc in pathwayAcc)
                    {
                        association.GeneList.Add(acc);
                    }

                    AddAssociation(association);

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
                        pathway.Selected = false;
                        pathway.SelectedKo.Clear();
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
            
            var accessionList = new List<string>();
            
            foreach (var protein in ProteinsToExport)
            {
                accessionList.Add(protein.Accession);
            }
            var accessionString = String.Join("+OR+", accessionList);
            //Console.WriteLine(accessionString);
            var allFastas = GetFastasFromNCBI(accessionString);

            var confirmationMessage = "FASTA file for selected genes written to C:\\Temp\\fasta.txt";

            MessageBox.Show(confirmationMessage, "FASTA Created", MessageBoxButton.OK);

        }

        private string GetFastasFromNCBI(string accessionString)
        {
            var fastas = "";

            var esearchURL =
                "http://eutils.ncbi.nlm.nih.gov/entrez/eutils/efetch.fcgi?db=nucleotide&id=" + accessionString + "&rettype=fasta&retmode=txt";//&usehistory=y";

            var esearchGetUrl = WebRequest.Create(esearchURL);

            //esearchGetUrl.Proxy = WebProxy.GetDefaultProxy();

            var getStream = esearchGetUrl.GetResponse().GetResponseStream();
            var reader = new StreamReader(getStream);
            var streamLine = "";
            while (streamLine != null)
            {
                streamLine = reader.ReadLine();
                if (streamLine != null)
                {
                    fastas += streamLine+'\n';
                }
            }
            fastas = fastas.Replace("\n\n", "\n");

            var outputpath = "C:\\Temp\\fasta.txt";

            if (File.Exists(outputpath))
            {
                File.Delete(outputpath);
            }

            using (var fastaWriter = new StreamWriter(outputpath))
            {
                fastaWriter.Write(fastas, 0, fastas.Length);
            }

            return fastas;
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
        
        public void AddToExport(ProteinInformation proteinToAdd)
        {
            ProteinsToExport.Add(proteinToAdd);
        }


        private void AddAssociation(OrganismPathwayProteinAssociation newAssociation)
        {
            var temp = PathwayProteinAssociation;
            var orgPathList = new Dictionary<string, List<string>>();
            foreach (var pair in temp)
            {
                if (!orgPathList.ContainsKey(pair.Organism))
                {
                    orgPathList.Add(pair.Organism, new List<string>());
                }
                orgPathList[pair.Organism].Add(pair.Pathway);
            }
            if (orgPathList.ContainsKey(newAssociation.Organism) &&
                orgPathList[newAssociation.Organism].Contains(newAssociation.Pathway))
            {
                var strippedTemp =
                    temp.Where(x => !(x.Organism == newAssociation.Organism && x.Pathway == newAssociation.Pathway));
                temp = new ObservableCollection<OrganismPathwayProteinAssociation>();
                foreach (var pair in strippedTemp)
                {
                    temp.Add(pair);
                }
            }
            temp.Add(newAssociation);
            PathwayProteinAssociation = temp;
        }    
    }
}