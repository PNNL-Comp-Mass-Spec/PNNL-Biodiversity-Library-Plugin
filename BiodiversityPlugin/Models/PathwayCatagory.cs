using System.Collections.Generic;

namespace BiodiversityPlugin.Models
{
    public class PathwayCatagory
    {
        /// <summary>
        /// KEGG Pathway Catagory 
        /// </summary>
        public string CatagoryName { get; set; }

        /// <summary>
        /// KEGG Id for the pathway (last 5 integers from map)
        /// </summary>
        public List<PathwayGroup> PathwayGroups { get; set; }

        /// <summary>
        /// Constructor which populates the group with the appropriate data
        /// </summary>
        /// <param name="catagoryName">Name of the pathway group (e.g. Energy Metabolism)</param>
        /// <param name="pathwayGroups">Groups that belong to the catagory (e.g. Photosynthesis)</param>
        public PathwayCatagory(string catagoryName, List<PathwayGroup> pathwayGroups)
        {
            CatagoryName = catagoryName;
            PathwayGroups = pathwayGroups;
        }

    }
}
