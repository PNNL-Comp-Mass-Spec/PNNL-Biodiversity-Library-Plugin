using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BiodiversityPlugin.Models
{
    public class OrganismWithFlag
    {
        public string OrganismName { get; set; }
        public bool Custom { get; set; }

        public OrganismWithFlag(string orgName, bool custom)
        {
            OrganismName = orgName;
            Custom = custom;
        }
    }
}
