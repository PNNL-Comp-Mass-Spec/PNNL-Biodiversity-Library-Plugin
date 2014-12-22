using System.Windows.Controls;
using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Shapes;
using Brushes = System.Windows.Media.Brushes;

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

        public void AddRectangle(int xCoord, int yCoord)
        {

            var rect = new System.Windows.Shapes.Rectangle
            {
                Width = 47,
                Height = 17,
                Fill = new SolidColorBrush(Colors.Red),
                Opacity = .50
            };
            PathwayCanvas.Children.Add(rect);
            Canvas.SetLeft(rect, xCoord);
            Canvas.SetTop(rect, yCoord);
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
        }
    }
}
