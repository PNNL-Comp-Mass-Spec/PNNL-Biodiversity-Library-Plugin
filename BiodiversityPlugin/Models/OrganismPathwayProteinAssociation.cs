using System.Collections.ObjectModel;
using GalaSoft.MvvmLight;

namespace BiodiversityPlugin.Models
{
    public class OrganismPathwayProteinAssociation : ViewModelBase
    {
        private bool _selected = true;
        
        public string Organism { get; set; }

        public string Pathway { get; set; }

        public ObservableCollection<ProteinInformation> GeneList { get; set; }

        /// <summary>
        /// Flag for if the Association is selected for export to FASTA
        /// </summary>
        public bool AssociationSelected
        {
            get { return _selected; }
            set
            {
                _selected = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Formated text for the organism for the View
        /// </summary>
        public string SelectedOrganismText
        {
            get { return string.Format("Organism: {0}", Organism); }
        }

        /// <summary>
        /// Formated text for the pathway for the View
        /// </summary>
        public string SelectedPathwayText
        {
            get { return string.Format("Pathway: {0}", Pathway); }
        }

        /// <summary>
        /// Formated text for the number of Genes for the View
        /// </summary>
        public string NumProteinsText
        {
            get { return string.Format("Genes ({0})", GeneList.Count); }
        }

    }
}
