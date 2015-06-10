using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BiodiversityPlugin.Models
{
    public enum DownloadLocationEnum
    {
        NONE, // Default value, will show up if error happens before download occurs
        MASSIVE_PUBLIC, // Default location for any non-human data
        MASSIVE_PRIVATE, // Used only if Public fails
        PNNL // Used Only if BOTH Public and private have been tried and failed OR if it's a human data
    }
}
