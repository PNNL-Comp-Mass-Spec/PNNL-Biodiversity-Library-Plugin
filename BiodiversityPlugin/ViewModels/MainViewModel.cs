using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Media;
using System.Xml.XPath;
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

        // temporary overview text for application
        private const string overviewText =
            "This Skyline plugin is designed to provide an easy interface for retrieving data from the Biodiversity Library developed"+
            "\n by Sam Payne at Pacific Northwest National Laboratory. "+
        "\n\nThe steps to follow for normal use are: "+
          "\n1) Select an Organism, either through the Phylum/Class explorer or through the Organism search box. "+
          "\n2) Select one or more Pathways, as defined by KEGG. "+
          "\n3) From here, a collection of the KEGG pathway images will be displayed with information pertaining to the organism "+
                "\n\thighlighted. Kegg Orthologs which are annotated for the organism but which are not present in the MS data " +
                "\n\twill be highlighted in blue, while orthologs which were observed are highlighted in red. You can deselect " +
                "\n\tany orthologs you are not interested in, denoted by a grey highlight, by clicking on the corresponding box " +
                "\n\ton the pathway. Once you are satisfied with your selection of orthologs in all the pathways you are interested "+
                "\n\tin, click the button to proceed to the review and export tab. " +
          "\n4) From this final tab, you are provided a list genes selected for each organism pathway pair and "+
                "\n\tare able to return to select additional organisms you are interested in. By hitting the export button, a FASTA file "+
                "\n\twill be created with the NCBI information for all the annotated genes selected.";

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

            var writer = new StreamWriter("C:\\Temp\\log.txt", true);
            writer.WriteLine("In VM constructor");
            writer.Close();
            
            var pwd = System.Reflection.Assembly.GetExecutingAssembly().CodeBase;
            var dir = System.IO.Path.GetDirectoryName(pwd).Substring(6); 
            writer = new StreamWriter("C:\\Temp\\log.txt", true);
            writer.WriteLine("Gotten pwd and dir");
            writer.WriteLine("pwd = " + pwd);
            writer.WriteLine("dir = " + dir);
            writer.Close();

            _proteins = PopulateProteins(proteinsPath);
            _dbPath = dbPath;
            writer = new StreamWriter("C:\\Temp\\log.txt", true);
            writer.WriteLine("Populated proteins and gotten db path");
            writer.Close();
            Messenger.Default.Register<PropertyChangedMessage<bool>>(this, PathwaysSelectedChanged);
            var organismList = new List<string>();
            var organisms = orgData.LoadOrganisms(ref organismList);
            writer = new StreamWriter("C:\\Temp\\log.txt", true);
            writer.WriteLine("Registered pathway selected message and loaded orgs");
            writer.Close();
            organismList.Sort();
            OrganismList = organismList;
            organisms.Sort((x, y) => x.PhylumName.CompareTo(y.PhylumName));
            Organisms = new ObservableCollection<OrgPhylum>(organisms);
            Pathways = new ObservableCollection<PathwayCatagory>(pathData.LoadPathways());
            writer = new StreamWriter("C:\\Temp\\log.txt", true);
            writer.WriteLine("Assigned orgs and loaded pathways");
            writer.Close();
            FilteredProteins = new ObservableCollection<ProteinInformation>();
            PreviousTabCommand = new RelayCommand(PreviousTab);
            NextTabCommand = new RelayCommand(NextTab);
            AcquireProteinsCommand = new RelayCommand(AcquireProteins);
            ExportToSkylineCommand = new RelayCommand(ExportToSkyline);
            DisplayPathwayImagesCommand = new RelayCommand(DisplayPathwayImages);
            SelectAdditionalOrganismCommand = new RelayCommand(SelectAdditionalOrganism);
            OrganismReviewCommand = new RelayCommand(OrganismReview);
            writer = new StreamWriter("C:\\Temp\\log.txt", true);
            writer.WriteLine("Assigned commands");
            writer.Close();
            _selectedTabIndex = 0;
            _isOrganismSelected = false;
            _isPathwaySelected = false;
            _visibleProteins = Visibility.Hidden;
            writer = new StreamWriter("C:\\Temp\\log.txt", true);
            writer.WriteLine("PreImageAssigning");
            writer.Close();
            _image = new ImageBrush();
            _imageString = null;//new Uri(string.Format("{0}\\DataFiles\\images\\map00010.png", dir),
                //UriKind.Absolute);
            _visiblePathway = Visibility.Hidden;
            writer = new StreamWriter("C:\\Temp\\log.txt", true);
            writer.WriteLine("PostImageString");
            writer.Close();
            _pathwaysSelected = 0;
            PathwayImage = _imageString;
            writer = new StreamWriter("C:\\Temp\\log.txt", true);
            writer.WriteLine("PostPathwayImage");
            writer.Close();
            _selectedPathways = new ObservableCollection<Pathway>();
            SelectedPathways = _selectedPathways;
            writer = new StreamWriter("C:\\Temp\\log.txt", true);
            writer.WriteLine("PostSelectedPathways");
            writer.Close();
            ProteinsToExport = new List<ProteinInformation>();
            PathwayProteinAssociation = new ObservableCollection<OrganismPathwayProteinAssociation>();
            OverviewText = overviewText;

            writer = new StreamWriter("C:\\Temp\\log.txt");
            writer.WriteLine("VM constructor complete", true);
            writer.Close();
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
            if (SelectedTabIndex == 1 && SelectedOrganism == null) return;
            // Do nothing if no selected pathway
            if (SelectedTabIndex == 2 && !IsPathwaySelected) return;
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

            var pwd = System.Reflection.Assembly.GetExecutingAssembly().CodeBase;
            var dir = System.IO.Path.GetDirectoryName(pwd);
            
            var dataAccess = new DatabaseDataLoader(_dbPath);
            var writer = new StreamWriter("blahblahblah.txt");
            writer.WriteLine(dir);
            var pieces = pwd.Split('\\');
            var absPath = dir.Substring(6);
            SelectedTabIndex++;
            writer.WriteLine(absPath);
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
                                new Uri(string.Format("{0}\\DataFiles\\images\\map{1}.png", dir, pathway.KeggId),
                                    UriKind.Absolute);
                            if (File.Exists(string.Format("{0}\\DataFiles\\images\\map{1}.png", dir, pathway.KeggId)))
                            {
                                writer.WriteLine("image found");
                            }
                            pathway.ClearRectangles();
                            writer.WriteLine(string.Format("{0}\\DataFiles\\coords\\path{1}KoCoords.txt",
                                            absPath,
                                            pathway.KeggId));
                            if (File.Exists(string.Format(string.Format("{0}\\DataFiles\\coords\\path{1}KoCoords.txt",
                                absPath,
                                pathway.KeggId))))
                            {
                                writer.WriteLine("Coords found");
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
            writer.Close();
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
            Console.WriteLine(allFastas);
        }

        private string GetFastasFromNCBI(string accessionString)
        {
            var fastas = "";

            var esearchURL =
                "http://eutils.ncbi.nlm.nih.gov/entrez/eutils/efetch.fcgi?db=nucleotide&id=" + accessionString + "&rettype=fasta&retmode=txt";//&usehistory=y";

            var esearchGetUrl = WebRequest.Create(esearchURL);

            esearchGetUrl.Proxy = WebProxy.GetDefaultProxy();

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
            var writer = new StreamWriter("C:\\Temp\\log.txt", true);
            writer.WriteLine("In PopulateProteins: input = " + fileName);
            writer.Close();

            if (File.Exists(fileName))
            {
                writer = new StreamWriter("C:\\Temp\\log.txt", true);
                writer.WriteLine("File exists");
                writer.Close();
            }
            else
            {
                writer = new StreamWriter("C:\\Temp\\log.txt", true);
                writer.WriteLine("File does not exist");
                writer.Close();
            }

            var file = File.ReadAllLines(fileName);
            writer = new StreamWriter("C:\\Temp\\log.txt", true);
            writer.WriteLine("After ReadAllLines");
            writer.Close();
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
        private string m_overviewText;
        private List<string> m_organismList;
        private string m_selectedValue;
        private ObservableCollection<OrganismPathwayProteinAssociation> m_pathwayProteinAssociation;

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
                if (OrganismList.Contains(value))
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
                                    return;
                                }
                            }
                        }
                    }
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
            //RaisePropertyChanged("PathwayProteinAssocation");
        }
    }
}