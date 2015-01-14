using System.Collections.Generic;
using BiodiversityPlugin.Models;

namespace BiodiversityPlugin.DataManagement
{
    public interface IDataAccess
    {
        List<OrgPhylum> LoadOrganisms(ref List<string> organismList );

        List<PathwayCatagory> LoadPathways();
    }
}
