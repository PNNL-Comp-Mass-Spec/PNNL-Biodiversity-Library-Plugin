using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BiodiversityPlugin
{
    public class DatabaseDataLoader: IDataAccess
    {
        public List<Models.OrgPhylum> LoadOrganisms(string path)
        {
            throw new NotImplementedException();
        }

        public List<Models.PathwayGroup> LoadPathways(string path)
        {
            throw new NotImplementedException();
        }
    }
}
