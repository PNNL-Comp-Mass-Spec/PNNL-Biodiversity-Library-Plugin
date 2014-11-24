using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BiodiversityPlugin.Models
{

    public class OrgPhylum
    {
        /// <summary>
        /// Name of the biological class
        /// </summary>
        public string PhylumName { get; set; }

        /// <summary>
        /// Organisms which are in that biological class
        /// </summary>
        public List<OrgClass> OrgClasses { get; set; }

        public OrgPhylum(string phylumName, List<OrgClass> classes)
        {
            PhylumName = phylumName;
            OrgClasses = classes;

        }
    }
}
