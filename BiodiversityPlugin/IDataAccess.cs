using System.Collections.Generic;
using BiodiversityPlugin.Models;

namespace BiodiversityPlugin
{
    public interface IDataAccess
    {
        List<OrgPhylum> LoadOrganisms();

        List<PathwayGroup> LoadPathways();
    }
}
