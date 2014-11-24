using System.Collections.Generic;
using BiodiversityPlugin.Models;

namespace BiodiversityPlugin
{
    public interface IDataAccess
    {
        List<OrgPhylum> LoadOrganisms(string path);

        List<PathwayGroup> LoadPathways(string path);
    }
}
