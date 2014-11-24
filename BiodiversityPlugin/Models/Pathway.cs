using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BiodiversityPlugin.Models
{
    /// <summary>
    /// Class for KEGG Pathways from Chris Overall's SQLite database
    /// </summary>
    public class Pathway
    {
        /// <summary>
        /// KEGG Pathway name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// KEGG Id for the pathway (last 5 integers from map)
        /// </summary>
        public int KeggId { get; set; }
    }
}
