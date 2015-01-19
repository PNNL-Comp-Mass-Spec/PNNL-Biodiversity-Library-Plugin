using System.Collections.Generic;
using BiodiversityPlugin.Models;

namespace BiodiversityPlugin.DataManagement
{
    public interface IDataAccess
    {
        /// <summary>
        /// Method by which organisms are loaded
        /// </summary>
        /// <param name="organismList">Output list of the organism names, used in display for the text-entry list</param>
        /// <returns>List of organisms, grouped by phylum, then by class, then organism</returns>
        List<OrgPhylum> LoadOrganisms(ref List<string> organismList );

        /// <summary>
        /// Method by which pathways are loaded
        /// </summary>
        /// <returns>List of Pathways, organized by Catagory, then by group, then individual pathway</returns>
        List<PathwayCatagory> LoadPathways();
    }
}
