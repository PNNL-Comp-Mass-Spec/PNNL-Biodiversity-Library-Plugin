using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BiodiversityPlugin.Models
{
    public enum ErrorTypeEnum
    {
        SkylineError, // Skyline version issue
        NcbiError, // NCBI connection issue (getting the FASTAs)
        MassiveError, // Spectral Library Issue (downloading the .blibs)
        None // Default, should never be this
    }
}
