using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BiodiversityPlugin.Models
{
    public class OrganismPathwayProteinAssociation
    {
        private string m_organism;
        private string m_pathway;
        private ObservableCollection<ProteinInformation> m_genes;

        public string Organism
        {
            get { return m_organism; }
            set { m_organism = value; }
        }

        public string Pathway
        {
            get { return m_pathway; }
            set { m_pathway = value; }
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
            get { return m_genes; }
            set { m_genes = value; }
        }
    }
}
