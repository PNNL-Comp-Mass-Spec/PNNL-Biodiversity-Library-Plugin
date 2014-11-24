using System;
using System.Collections.Generic;
using BiodiversityPlugin.Models;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BiodiversityPlugin
{
    class HardCodedData:IDataAccess
    {

        public List<OrgPhylum> LoadOrganisms(string path)
        {
            var toReturn = new List<OrgPhylum>();

            var phylumClasses = new List<OrgClass>();

            var organisms = new List<Organism>();
            organisms.Add(new Organism("Bacillus_subtilis_168", 1234)); // NOT ACTUAL TAXON
            organisms.Add(new Organism("Staphylococcus_aureus_ATCC33591", 1235)); // NOT ACTUAL TAXON
            organisms.Add(new Organism("Bacillus_anthracis_Sterne", 1236)); // NOT ACTUAL TAXON
            organisms.Add(new Organism("Bacillus_anthracis_Ames", 1237)); // NOT ACTUAL TAXON

            phylumClasses.Add(new OrgClass("Bacilli", organisms));
            
            organisms = new List<Organism>();
            organisms.Add(new Organism("Clostridium_thermocellum", 1238)); // NOT ACTUAL TAXON
            organisms.Add(new Organism("Dethiosulfovibrio_peptidovorans_DSM_11002", 1214)); // NOT ACTUAL TAXON
            organisms.Add(new Organism("Heliobacterium_modesticaldum", 1244)); // NOT ACTUAL TAXON

            phylumClasses.Add(new OrgClass("Clostridia", organisms));

            toReturn.Add(new OrgPhylum("Firmicutes", phylumClasses));

            phylumClasses = new List<OrgClass>();
            organisms = new List<Organism>();
            organisms.Add(new Organism("Haloferax_volcanii", 1111)); // NOT ACTUAL TAXON
            phylumClasses.Add(new OrgClass("Halobacteria", organisms));
            toReturn.Add(new OrgPhylum("Euryarchaeota", phylumClasses));

            return toReturn;
        }

        public List<PathwayGroup> LoadPathways(string path)
        {

            var pathways = new List<Pathway>();
            pathways.Add(new Pathway("Glycolysis", 00010));
            pathways.Add(new Pathway("Citrate Cycle (TCA cycle)", 00020));

            var toReturn = new List<PathwayGroup>();

            var group = new PathwayGroup("Carbohydrate metabolism", pathways);

            toReturn.Add(group);
            return toReturn;
        }
    }
}
