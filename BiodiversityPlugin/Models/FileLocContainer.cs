using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BiodiversityPlugin.Models
{
    public class FileLocContainer
    {
        public string OrgName { get; set; }
        public string FileLocation { get; set; }
        public bool IsCustom { get; set; }

        public FileLocContainer(string orgName, string fileLoc, bool custom)
        {
            OrgName = orgName;
            FileLocation = fileLoc;
            IsCustom = custom;
        }
    }
}
