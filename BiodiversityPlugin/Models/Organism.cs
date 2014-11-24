using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BiodiversityPlugin.Models
{
    /// <summary>
    /// Class for Organisms from Chris Overall's SQLite database
    /// </summary>
    public class Organism
    {
        /// <summary>
        /// Organism name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Organism's NCBI taxon id
        /// </summary>
        public int Taxon { get; set; }
    }

}
