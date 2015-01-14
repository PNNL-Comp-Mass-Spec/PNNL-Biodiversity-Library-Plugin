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

        /// <summary>
        /// Organism's KEGG OrgCode
        /// </summary>
        public string OrgCode { get; set; }

        public Organism(string name, int taxon, string orgCode)
        {
            Name = name;
            Taxon = taxon;
            OrgCode = orgCode;
        }
    }
}
