using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BiodiversityPlugin.Models
{
    public class PathwayGroup
    {
        /// <summary>
        /// KEGG Pathway name
        /// </summary>
        public string GroupName { get; set; }

        /// <summary>
        /// KEGG Id for the pathway (last 5 integers from map)
        /// </summary>
        public List<Pathway> Pathways  { get; set; }

        /// <summary>
        /// Constructor which populates the group with the appropriate data
        /// </summary>
        /// <param name="groupName">Name of the pathway group (e.g. Energy Metabolism)</param>
        /// <param name="groupPathways">Pathways that belong to the group (e.g. Photosynthesis)</param>
        public PathwayGroup(string groupName, List<Pathway> groupPathways)
        {
            GroupName = groupName;
            Pathways = groupPathways;
        }

    }
}
