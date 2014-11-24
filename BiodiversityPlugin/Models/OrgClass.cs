using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BiodiversityPlugin.Models
{
    public class OrgClass
    {
        /// <summary>
        /// Name of the biological class
        /// </summary>
        public string ClassName { get; set; }

        /// <summary>
        /// Organisms which are in that biological class
        /// </summary>
        public List<Organism> Organisms { get; set; }

        public OrgClass(string className, List<Organism> organisms)
        {
            ClassName = className;
            Organisms = organisms;

        }
    }
}
