namespace BiodiversityPlugin.Models
{
    /// <summary>
    /// Class for the information for a Kegg Ortholog, containing the KoID number,
    /// Kegg Gene Name, and Kegg Ec.
    /// </summary>
    public class KeggKoInformation
    {
        public string KeggKoId { get; set; }

        public string KeggGeneName { get; set; }

        public string KeggEc { get; set; }

        public KeggKoInformation(string id, string name, string ec)
        {
            KeggKoId = id;
            KeggGeneName = name;
            KeggEc = ec;
        }
    }
}
