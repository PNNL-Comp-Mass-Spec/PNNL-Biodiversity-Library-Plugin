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
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using BiodiversityPlugin.Models;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;

namespace BiodiversityPlugin.ViewModels
{

    public class MainViewModel: ViewModelBase
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

        //public ImageBrush PathwayImage
        //{
        //    get { return _image; }
        //    private set
        //    {
        //        _image = value;
        //        PathwayVisibility = Visibility.Hidden;
        //        if (_image.ImageSource != null)
        //        {
        //            PathwayVisibility = Visibility.Visible;
        //        }
        //        RaisePropertyChanged();
        //    }
        //}

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

                NumProteinsText = string.Format("Proteins ({0})", m_filteredProteins.Count);
                VisibleProteins = m_filteredProteins.Count > 0 ? Visibility.Visible : Visibility.Hidden;
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

        public int SelectedTabIndex
        {
            get { return _selectedTabIndex; }
            set
            {
                _selectedTabIndex = value;
                RaisePropertyChanged();
            }
        }

        public RelayCommand ExportToSkylineCommand { get; private set; }

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
            ExportToSkylineCommand = new RelayCommand(ExportToSkyline);
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

        private void PathwaysSelectedChanged(PropertyChangedMessage<bool> message)
        {
            if (message.PropertyName == "Selected" && message.Sender is Pathway)
            {
                if(message.NewValue == true)
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

        private void ExportToSkyline()
        {
            IsQuerying = true;

            var pwd = Directory.GetCurrentDirectory();
            var pieces = pwd.Split('\\');
            var absPath = "";
            for (var i = 0; i < pieces.Count() - 3; i++)
            {
                absPath += string.Format("{0}{1}", pieces[i], '\\');
            }
            SelectedTabIndex++;
            var selectedPaths = new List<Pathway>();
            foreach(var catagory in Pathways)
            {
                foreach(var group in catagory.PathwayGroups)
                {
                    foreach(var pathway in group.Pathways)
                    {
                        if (pathway.Selected)
                        {
                            pathway.PathwayImage = new Uri(string.Format("{0}resources\\images\\map{1}.png", absPath, pathway.KeggId), UriKind.Absolute);
                            selectedPaths.Add(pathway);
                            if (selectedPaths.Count == 1)
                            {
                                SelectedPathwayText = string.Format("Pathway: {0}", pathway.Name);
                            }
                            else if (selectedPaths.Count % 4 == 0)
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

            if (SelectedTabIndex == 3)
            {
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
                        QueryString = queryingStrings[index%4];
                        index++;
                    }
                });
                var accessions = new List<ProteinInformation>();
                    if (SelectedPathway != null && SelectedOrganism != null)
                    {

                        var dataAccess = new DatabaseDataLoader(_dbPath);
                        //var accessions = dataAccess.ExportAccessions(SelectedPathway, SelectedOrganism);
                        var temp = dataAccess.ExportAccessions(selectedPaths, SelectedOrganism);
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
                        }
                    }
                }

				//if (SelectedPathway.KeggId == "00010" || SelectedPathway.KeggId == "00020" ||
				//	SelectedPathway.KeggId == "00195")
				//{
				//	var thing =
				//		File.Exists(string.Format("..\\..\\..\\resources\\images\\map{0}.png", SelectedPathway.KeggId));
				//	var dirThing = Directory.Exists("..\\..\\..\\resources\\images");
				//	var imageSource =
				//		new BitmapImage(
				//			new Uri(string.Format("..\\..\\..\\resources\\images\\map{0}.png", SelectedPathway.KeggId),
				//				UriKind.Relative));
				//	PathwayImage =
				//		new Uri(string.Format("{0}resources\\images\\map{1}.png", absPath, SelectedPathway.KeggId),
				//			UriKind.Absolute);
				//		//string.Format("/BiodiversityPlugin;component\\..\\..\\..\\resources\\images\\map{0}.png", SelectedPathway.KeggId);
				//	PathwayVisibility = Visibility.Visible;
				//}
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
        private  bool _isQuerying;
        private string _queryString;
        private int _selectedTabIndex;
        private ImageBrush _image;
        private Visibility _visiblePathway;
        private int _pathwaysSelected;
        private Uri _imageString;
        private ObservableCollection<Pathway> _selectedPathways;


        private List<string> _protNames = new List<string>();

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
