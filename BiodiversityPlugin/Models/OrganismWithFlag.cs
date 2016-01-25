using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SQLite;
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

        public static ObservableCollection<OrganismWithFlag> ConvertToFlaggedList(List<string> InputOrganisms, string databasePath)
        {
            var fileLocSource = databasePath.Replace("PBL.db", "blibFileLoc.db");

            ObservableCollection<OrganismWithFlag> orgCollection = new ObservableCollection<OrganismWithFlag>();
            using (var dbConnection = new SQLiteConnection("Datasource=" + fileLocSource + ";Version=3;"))
            {
                dbConnection.Open();
                using (var cmd = new SQLiteCommand(dbConnection))
                {
                    foreach (var org in InputOrganisms)
                    {
                        var text = " SELECT * FROM customOrganisms WHERE orgName = \"" + org + "\"";
                        cmd.CommandText = text;
                        SQLiteDataReader reader = cmd.ExecuteReader();
                        if (reader.Read())
                        {
                            orgCollection.Add(new OrganismWithFlag(org, true));
                        }
                        else
                        {
                            orgCollection.Add(new OrganismWithFlag(org, false));
                        }
                        reader.Close();
                    }
                }
            }
            return orgCollection;
        }  
    }
}
