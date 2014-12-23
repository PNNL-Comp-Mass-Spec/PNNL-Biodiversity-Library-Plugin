using System.Windows.Controls;
using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Drawing;
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
	    private Canvas _pathwayCanvas;
        private List<String> _selectedKo;

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

        /// <summary>
        /// KEGG Pathway name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// KEGG Id for the pathway
        /// </summary>
        public string KeggId { get; set; }

        public void SelectAll()
        {
            foreach (var child in PathwayCanvas.Children)
            {
                Select(child);
            }
        }

        public void DeselectAll()
        {
            foreach (var child in PathwayCanvas.Children)
            {
                Deselect(child);
            }
        }

        public void ClearRectangles()
        {
            PathwayCanvas.Children.Clear();
        }

        private void Deselect(object child)
        {
            var rect = child as System.Windows.Shapes.Rectangle;
            rect.Fill = new SolidColorBrush(Colors.Gray);
            SelectedKo.Clear();
        }

        private void Select(object child)
        {
            var rect = child as System.Windows.Shapes.Rectangle;
            rect.Fill = new SolidColorBrush(Colors.Red);

            var koName = rect.Tag as string;
            SelectedKo.Clear();

            var kos = koName.Split(',');
            foreach (var ko in kos)
            {
                var trimmedko = ko.Trim();
                SelectedKo.Add(trimmedko);
            }
        }

        public void AddRectangle(string koName, int xCoord, int yCoord)
        {

            var rect = new System.Windows.Shapes.Rectangle
            {
                Tag = koName,
                ToolTip = koName,
                Width = 47,
                Height = 17,
                Fill = new SolidColorBrush(Colors.Red),
                Opacity = .50
            };
            rect.MouseDown += rect_MouseDown;
            PathwayCanvas.Children.Add(rect);
            Canvas.SetLeft(rect, xCoord);
            Canvas.SetTop(rect, yCoord);
            var kos = koName.Split(',');
            foreach (var ko in kos)
            {
                var trimmedko = ko.Trim();
                SelectedKo.Add(trimmedko);
            }
        }

        void rect_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var parent = sender as System.Windows.Shapes.Rectangle; 
            var koName = parent.Tag as string;
            var color = parent.Fill;
            if (((SolidColorBrush) color).Color == Colors.Red)
            {
                parent.Fill = new SolidColorBrush(Colors.Gray);
                var kos = koName.Split(',');
                foreach (var ko in kos)
                {
                    var trimmedko = ko.Trim();
                    SelectedKo.Remove(trimmedko);
                }
            }
            else
            {
                parent.Fill = new SolidColorBrush(Colors.Red);
                var kos = koName.Split(',');
                foreach (var ko in kos)
                {
                    var trimmedko = ko.Trim();
                    SelectedKo.Add(trimmedko);
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
            SelectedKo = new List<string>();
            SelectAllCommand = new RelayCommand(SelectAll);
            DeselectAllCommand = new RelayCommand(DeselectAll);
        }

        public RelayCommand SelectAllCommand { get; set; }

        public RelayCommand DeselectAllCommand { get; set; }
    }
}
