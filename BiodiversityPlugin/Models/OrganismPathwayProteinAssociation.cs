using System.Collections.ObjectModel;

namespace BiodiversityPlugin.Models
{
    public class OrganismPathwayProteinAssociation
    {
        private string _organism;
        private string _pathway;
        private ObservableCollection<ProteinInformation> _genes;

        public string Organism
        {
            get { return _organism; }
            set { _organism = value; }
        }

        public string Pathway
        {
            get { return _pathway; }
            set { _pathway = value; }
        }

        public string SelectedOrganismText
        {
            get { return string.Format("Organism: {0}", Organism); }
        }

        public string SelectedPathwayText
        {
            get { return string.Format("Pathway: {0}", Pathway); }
        }

        public string NumProteinsText
        {
            get { return string.Format("Genes ({0})", GeneList.Count); }
        }

        public ObservableCollection<ProteinInformation> GeneList
        {
            get { return _genes; }
            set { _genes = value; }
        }
    }
}
