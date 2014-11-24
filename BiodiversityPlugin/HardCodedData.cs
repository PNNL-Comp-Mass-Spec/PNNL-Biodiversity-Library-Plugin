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

        public List<OrgPhylum> LoadOrganisms()
        {
            var toReturn = new List<OrgPhylum>();

            throw new NotImplementedException();
        }

        public List<PathwayGroup> LoadPathways()
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
