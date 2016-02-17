using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BiodiversityPlugin.Models
{
    public enum TaskSelectionEnum
    {
        NONE, //Default value
        REPLACE, //Replace the existing organism's data with the user's personal data
        SUPPLEMENT, //Combine the user's personal data with the existing data for the organism
        INSERT_NEW //Insert a new organism that does not exist in the database yet
    }
}
