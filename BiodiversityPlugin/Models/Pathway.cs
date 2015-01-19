using System.Windows.Controls;
using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Xml;
using GalaSoft.MvvmLight.CommandWpf;

namespace BiodiversityPlugin.Models 
{
    /// <summary>
    /// Class for KEGG Pathways from Chris Overall's SQLite database
    /// </summary>
    public class Pathway : ViewModelBase
    {
        private Color _selectedColor = Colors.Red;

        private bool m_selected;
        private Uri _imageString;
	    private Canvas _pathwayDataCanvas;
        private Canvas _pathwayCanvas;
        private List<String> _selectedKo;
        private string m_informationMessage;
        private int m_numDataBoxes;
        private int m_dataBoxesSelected;
        private string m_legendSource;
        private string m_keggReqId;
        
        public string InformationMessage
        {
            get { return m_informationMessage; }
            set
            {
                m_informationMessage = value;
                RaisePropertyChanged("InformationMessage");
            }
        }

        /// <summary>
        /// Whether the pathway is selected by the user or not.
        /// </summary>
        public bool Selected
        {
            get { return m_selected; }
            set
            {
                var oldSelected = m_selected;
                m_selected = value;
                RaisePropertyChanged("Selected", oldSelected, m_selected, true);
            }
        }

        /// <summary>
        /// List of Kegg Orthologs selected within the pathway
        /// </summary>
        public List<String> SelectedKo
        {
            get { return _selectedKo; }
            set { _selectedKo = value; }
        }

        /// <summary>
        /// Image for the pathway
        /// </summary>
        public Uri PathwayImage
        {
            get { return _imageString; }
            set
            {
                _imageString = value;
                RaisePropertyChanged();
                var temp = LegendSource;
                LegendSource = temp;
            }
        }

        /// <summary>
        /// Canvas for anything drawn that user cannot interact with.
        /// </summary>
		public Canvas PathwayNonDataCanvas
		{
			get { return _pathwayCanvas; }
			private set
			{
				_pathwayCanvas = value;
				RaisePropertyChanged();
			}
		}

        /// <summary>
        /// Canvas for anything drawn user can interact with:
        /// e.g. Kegg Ortholog boxes for selection that have data from MSMS
        /// </summary>
        public Canvas PathwayDataCanvas
        {
            get { return _pathwayDataCanvas; }
            private set
            {
                _pathwayDataCanvas = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// KEGG Pathway name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// KEGG Id for the pathway
        /// </summary>
        public string KeggId { get; set; }

        private void UpdateMessage()
        {
            if (m_dataBoxesSelected == m_numDataBoxes)
            {
                InformationMessage = "All Proteins Selected";
            }
            else if (m_dataBoxesSelected == 0)
            {
                InformationMessage = "All Proteins Deselected";
            }
            else
            {
                InformationMessage = "";
            }
        }

        /// <summary>
        /// Method to select all the members in the Data Canvas which adds all
        /// of the Kegg Orthologs to the list of selected Orthologs
        /// </summary>
        public void SelectAll()
        {
            foreach (var child in PathwayDataCanvas.Children)
            {
                Select(child);
            }
            m_dataBoxesSelected = m_numDataBoxes;
            UpdateMessage();
        }


        /// <summary>
        /// Method to deselect all the members in the Data Canvas which clears
        /// the list of selected Orthologs
        /// </summary>
        public void DeselectAll()
        {
            foreach (var child in PathwayDataCanvas.Children)
            {
                Deselect(child);
            }
            m_dataBoxesSelected = 0;
            UpdateMessage();
        }

        /// <summary>
        /// Used to remove everything from the canvases and reset the information
        /// related to them
        /// </summary>
        public void ClearRectangles()
        {
            PathwayDataCanvas.Children.Clear();
            PathwayNonDataCanvas.Children.Clear();
            m_dataBoxesSelected = 0;
            m_numDataBoxes = 0;
        }

        /// <summary>
        /// Change the child to deselected. This alters the color of the child to
        /// Gray, grabs the string Tag for the child, splits it into individual instances
        /// and removes those from the list of selected Orthologs.
        /// </summary>
        /// <param name="child">The rectangle that is clicked with a string tag.</param>
        private void Deselect(object child)
        {
            // Shape must be a rectangle.
            var rect = child as System.Windows.Shapes.Rectangle;
            if (rect == null) return;
            // Tag must be a string
            var koName = rect.Tag as string;
            if (koName == null) return;

            rect.Fill = new SolidColorBrush(Colors.Gray);
            m_dataBoxesSelected--;
            UpdateMessage();

            var kos = koName.Split(',');
            foreach (var ko in kos)
            {
                var trimmedko = ko.Trim();
                if (SelectedKo.Contains(trimmedko))
                    SelectedKo.Remove(trimmedko);
            }
        }


        /// <summary>
        /// Change the child to selected. This alters the color of the child to
        /// Red, grabs the string Tag for the child, splits it into individual instances
        /// and adds those to the list of selected Orthologs if they are not already in the
        /// list of selected orthologs.
        /// </summary>
        /// <param name="child">The rectangle that is clicked with a string tag.</param>
        private void Select(object child)
        {
            // Shape must be a rectangle.
            var rect = child as System.Windows.Shapes.Rectangle;
            if (rect == null) return;
            // Tag must be a string
            var koName = rect.Tag as string;
            if (koName == null) return;
            rect.Fill = new SolidColorBrush(Colors.Red);

            m_dataBoxesSelected++;
            UpdateMessage();
            
            var kos = koName.Split(',');
            foreach (var ko in kos)
            {
                var trimmedko = ko.Trim();
                if(!SelectedKo.Contains(trimmedko))
                    SelectedKo.Add(trimmedko);
            }
        }

        public void AddRectangle(List<KeggKoInformation> koInformation, int xCoord, int yCoord, bool isData)
        {
            if (isData)
            {
                AddDataRectangle(koInformation, xCoord, yCoord);
            }
            else
            {
                AddNonDataRectangle(koInformation, xCoord, yCoord);
            }
        }

        public void AddRectangle(List<KeggKoInformation> koInformation, int xCoord, int yCoord, bool isData, Color color)
        {
            var child = isData ? 
                            AddDataRectangle(koInformation, xCoord, yCoord) :
                            AddNonDataRectangle(koInformation, xCoord, yCoord);
            child.Fill = new SolidColorBrush(color);
        }

        private Rectangle AddDataRectangle(List<KeggKoInformation> koInformation, int xCoord, int yCoord)
        {
            var koIds = koInformation.First().KeggKoId;
            var keggGeneNames = koInformation.First().KeggGeneName;
            var keggEcs = koInformation.First().KeggEc;
            
            foreach(var ko in koInformation)
                if (ko != koInformation.First())
                {
                    koIds += ", " + ko.KeggKoId;
                    keggGeneNames += ", " + ko.KeggGeneName;
                    keggEcs += ", " + ko.KeggEc;
                }

            var tooltip = string.Format("{0}\nGene Name: {1}\nKegg Ec: {2}", koIds,
                keggGeneNames, keggEcs);
            var rect = new System.Windows.Shapes.Rectangle
            {
                Tag = koIds,
                ToolTip = tooltip,
                Width = 47,
                Height = 17,
                Opacity = .50
            };
            rect.MouseDown += rect_MouseDown;
            PathwayDataCanvas.Children.Add(rect);
            Canvas.SetLeft(rect, xCoord);
            Canvas.SetTop(rect, yCoord);
            m_numDataBoxes++;
            Select(rect);

            UpdateMessage();
            return rect;
        }

        private Rectangle AddNonDataRectangle(List<KeggKoInformation> koInformation, int xCoord, int yCoord)
        {
            var koIds = koInformation.First().KeggKoId;
            var keggGeneNames = koInformation.First().KeggGeneName;
            var keggEcs = koInformation.First().KeggEc;

            foreach (var ko in koInformation)
                if (ko != koInformation.First())
                {
                    koIds += ", " + ko.KeggKoId;
                    keggGeneNames += ", " + ko.KeggGeneName;
                    keggEcs += ", " + ko.KeggEc;
                }

            var tooltip = string.Format("{0}\nGene Name: {1}\nKegg Ec: {2}", koIds,
                keggGeneNames, keggEcs);
            var rect = new System.Windows.Shapes.Rectangle
            {
                Tag = koIds,
                ToolTip = tooltip,
                Width = 47,
                Height = 17,
                Fill = new SolidColorBrush(Colors.Blue),
                Opacity = .50
            };
            PathwayNonDataCanvas.Children.Add(rect);
            Canvas.SetLeft(rect, xCoord);
            Canvas.SetTop(rect, yCoord);
            return rect;
        }

        void rect_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var parent = sender as System.Windows.Shapes.Rectangle; 
            var koName = parent.Tag as string;
            var color = parent.Fill;
            if (((SolidColorBrush) color).Color == Colors.Red)
            {
                Deselect(parent);
                //parent.Fill = new SolidColorBrush(Colors.Gray);
                var kos = koName.Split(',');
                foreach (var ko in kos)
                {
                    var trimmedko = ko.Trim();
                    //SelectedKo.Remove(trimmedko);
                    foreach (var child in PathwayDataCanvas.Children)
                    {
                        var rect = child as System.Windows.Shapes.Rectangle;
                        var rectTag = rect.Tag as string;
                        var rectColor = rect.Fill;
                        if (rect != sender && rectTag.Contains(trimmedko) && ((SolidColorBrush)rectColor).Color == Colors.Red)
                        {
                            Deselect(rect);
                        }
                    }
                }
            }
            else
            {
                Select(parent);
                //parent.Fill = new SolidColorBrush(Colors.Red);
                var kos = koName.Split(',');

                foreach (var ko in kos)
                {
                    var trimmedko = ko.Trim();
                    //SelectedKo.Add(trimmedko);
                    foreach (var child in PathwayDataCanvas.Children)
                    {
                        var rect = child as System.Windows.Shapes.Rectangle;
                        var rectTag = rect.Tag as string;
                        var rectColor = rect.Fill;
                        if (rect != sender && rectTag.Contains(trimmedko) && ((SolidColorBrush)rectColor).Color == Colors.Gray)
                        {
                            Select(rect);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Constructor to populate with necessary data
        /// </summary>
        /// <param name="name">Name of the pathway (e.g. glycolysis)</param>
        /// <param name="keggId">Integer Id</param>
        public Pathway(string name, string keggId)
        {
            Name = name;
            KeggId = keggId;
            m_selected = false;
			PathwayNonDataCanvas = new Canvas();
            PathwayDataCanvas = new Canvas();
            SelectedKo = new List<string>();
            SelectAllCommand = new RelayCommand(SelectAll);
            DeselectAllCommand = new RelayCommand(DeselectAll);
            UpdateMessage();
            m_numDataBoxes = 0;
            m_dataBoxesSelected = 0;
            LegendSource = "Datafiles/pathwayLegend.png";
        }

        public RelayCommand SelectAllCommand { get; set; }

        public RelayCommand DeselectAllCommand { get; set; }

        public string LegendSource { 
            get { return m_legendSource; } 
            set 
            { 
                m_legendSource = value; 
                RaisePropertyChanged(); 
            } 
        }

        internal void WriteFoundText(int p1, int p2, string orgName)
        {
            var textBlock = new TextBlock();
            textBlock.Text = "Protein annotated in " + orgName + " and observed in MS/MS data";
            textBlock.FontSize = 12;
            PathwayNonDataCanvas.Children.Add(textBlock);
            Canvas.SetLeft(textBlock, p1 + 50);
            Canvas.SetTop(textBlock, p2);
        }

        internal void WriteNotfoundText(int p1, int p2, string orgName)
        {
            var textBlock = new TextBlock();
            textBlock.Text = "Protein annotated in " + orgName + " and not observed in MS/MS data";
            textBlock.FontSize = 12;
            PathwayNonDataCanvas.Children.Add(textBlock);
            Canvas.SetLeft(textBlock, p1 + 50);
            Canvas.SetTop(textBlock, p2);
        }

        internal void LoadImage()
        {
            try
            {

                var pathwayline = "";
                var esearchURL = string.Format("http://rest.kegg.jp/find/pathway/{0}", Name.Split('/').First().TrimEnd().Replace(" ", "%20"));
                //var esearchGetUrl = WebRequest.Create(esearchURL);
                //var getStream = esearchGetUrl.GetResponse().GetResponseStream();
                //var reader = new StreamReader(getStream);
                //pathwayline = reader.ReadLine();
                m_keggReqId = pathwayline.Substring(8, 5);
                PathwayImage = new Uri(string.Format("C:\\Temp\\PullerDownload\\Images\\map{0}.png", m_keggReqId), UriKind.RelativeOrAbsolute);
                //reader.Close();
                //esearchGetUrl.Abort();
            }
            catch (Exception)
            {
                m_keggReqId = KeggId;
                PathwayImage = new Uri(string.Format("http://rest.kegg.jp/get/map{0}/image", KeggId));
            }
        }

        internal Dictionary<string, List<Tuple<int, int>>> LoadCoordinates()
        {
            var coordDict = new Dictionary<string, List<Tuple<int, int>>>();
            var xml = "";
            var xmlSettings = new XmlReaderSettings();
            xmlSettings.DtdProcessing = DtdProcessing.Ignore;

            //var esearchURL = string.Format("http://rest.kegg.jp/get/ko{0}/kgml", m_keggReqId);

            //var esearchGetUrl = WebRequest.Create(esearchURL);

            //esearchGetUrl.Proxy = WebProxy.GetDefaultProxy();

            //var getStream = esearchGetUrl.GetResponse().GetResponseStream();
            //var reader = new StreamReader(getStream);
            
            var path = string.Format("C:\\Temp\\PullerDownload\\Coords\\path{0}.xml", m_keggReqId);

            //var xmlRead = XmlReader.Create(esearchURL, settings);
            var xmlRead = XmlReader.Create(path, xmlSettings);           
            
            xmlRead.ReadToFollowing("pathway");
            while (xmlRead.ReadToFollowing("entry"))
            {
                var wholeName = xmlRead.GetAttribute("name");
                var backup = wholeName.Split(' ');
                var pieces = new List<string>();
                foreach (var piece in backup)
                {
                    pieces.Add(piece.Split(':').Last());
                }
                xmlRead.ReadToFollowing("graphics");
                var type = xmlRead.GetAttribute("type");
                var x = Convert.ToInt32(xmlRead.GetAttribute("x")) - (Convert.ToInt32(xmlRead.GetAttribute("width")) / 2);
                var y = Convert.ToInt32(xmlRead.GetAttribute("y")) - (Convert.ToInt32(xmlRead.GetAttribute("height")) / 2);
                if (type == "rectangle")
                {
                    foreach (var piece in pieces)
                    {
                        if (piece.StartsWith("K"))
                        {
                            if (!coordDict.ContainsKey(piece))
                            {
                                coordDict[piece] = new List<Tuple<int, int>>();
                            }
                            coordDict[piece].Add(new Tuple<int, int>(x, y));
                        }
                    }
                }
            }
            //reader.Close();
            //esearchGetUrl.Abort();

            return coordDict;

        }
    }
}
