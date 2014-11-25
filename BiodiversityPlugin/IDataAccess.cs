using System.Collections.Generic;
using BiodiversityPlugin.Models;

namespace BiodiversityPlugin
{
    public interface IDataAccess
    {
        List<OrgPhylum> LoadOrganisms(string path);

        List<PathwayCatagory> LoadPathways(string path);
    }
}
