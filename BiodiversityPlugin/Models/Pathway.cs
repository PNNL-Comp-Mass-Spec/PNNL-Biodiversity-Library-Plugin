using System.Windows.Controls;
using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Xml.Serialization;
using Brushes = System.Windows.Media.Brushes;
using GalaSoft.MvvmLight.CommandWpf;

namespace BiodiversityPlugin.Models 
{
    /// <summary>
    /// Class for KEGG Pathways from Chris Overall's SQLite database
    /// </summary>
    public class Pathway : ViewModelBase 
    {
        private bool m_selected;
        private Uri _imageString;
	    private Canvas _pathwayDataCanvas;
        private Canvas _pathwayCanvas;
        private List<String> _selectedKo;
        private string m_informationMessage;
        private int m_numDataBoxes;
        private int m_dataBoxesSelected;
        private string m_legendSource;

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

        public List<String> SelectedKo
        {
            get { return _selectedKo; }
            set { _selectedKo = value; }
        }

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

		public Canvas PathwayCanvas
		{
			get { return _pathwayCanvas; }
			private set
			{
				_pathwayCanvas = value;
				RaisePropertyChanged();
			}
		}

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

        public void SelectAll()
        {
            foreach (var child in PathwayDataCanvas.Children)
            {
                Select(child);
            }
            m_dataBoxesSelected = m_numDataBoxes;
            UpdateMessage();
        }

        public void DeselectAll()
        {
            foreach (var child in PathwayDataCanvas.Children)
            {
                Deselect(child);
            }
            m_dataBoxesSelected = 0;
            UpdateMessage();
        }

        public void ClearRectangles()
        {
            PathwayDataCanvas.Children.Clear();
            PathwayCanvas.Children.Clear();
            m_dataBoxesSelected = 0;
            m_numDataBoxes = 0;
        }

        private void Deselect(object child)
        {
            var rect = child as System.Windows.Shapes.Rectangle;
            rect.Fill = new SolidColorBrush(Colors.Gray);
            var koName = rect.Tag as string;
            //SelectedKo.Clear();
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

        private void Select(object child)
        {
            var rect = child as System.Windows.Shapes.Rectangle;
            rect.Fill = new SolidColorBrush(Colors.Red);

            var koName = rect.Tag as string;
            //SelectedKo.Clear();
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

        public void AddDataRectangle(List<KeggKoInformation> koInformation, int xCoord, int yCoord, System.Windows.Media.Color color)
        {
            //coord.Value.Aggregate((working, next) => working + ", " + next)
            var koIds = koInformation.First().KeggKoId;// koInformation.Aggregate((working, next) => working.KeggKoId + ", " + next.KeggKoId);
            var keggGeneNames = koInformation.First().KeggGeneName;// koInformation
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
                Fill = new SolidColorBrush(color),
                Opacity = .50
            };
            rect.MouseDown += rect_MouseDown;
            PathwayDataCanvas.Children.Add(rect);
            Canvas.SetLeft(rect, xCoord);
            Canvas.SetTop(rect, yCoord);
            m_numDataBoxes++;
            Select(rect);

            UpdateMessage();
        }

        public void AddRectangle(List<KeggKoInformation> koInformation, int xCoord, int yCoord, System.Windows.Media.Color color)
        {
            //coord.Value.Aggregate((working, next) => working + ", " + next)
            var koIds = koInformation.First().KeggKoId;// koInformation.Aggregate((working, next) => working.KeggKoId + ", " + next.KeggKoId);
            var keggGeneNames = koInformation.First().KeggGeneName;// koInformation
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
                Fill = new SolidColorBrush(color),
                Opacity = .50
            };
            PathwayCanvas.Children.Add(rect);
            Canvas.SetLeft(rect, xCoord);
            Canvas.SetTop(rect, yCoord);
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
			PathwayCanvas = new Canvas();
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
            PathwayCanvas.Children.Add(textBlock);
            Canvas.SetLeft(textBlock, p1 + 50);
            Canvas.SetTop(textBlock, p2);
        }

        internal void WriteNotfoundText(int p1, int p2, string orgName)
        {
            var textBlock = new TextBlock();
            textBlock.Text = "Protein annotated in " + orgName + " and not observed in MS/MS data";
            textBlock.FontSize = 12;
            PathwayCanvas.Children.Add(textBlock);
            Canvas.SetLeft(textBlock, p1 + 50);
            Canvas.SetTop(textBlock, p2);
        }
    }
}
