namespace BiodiversityPlugin.Models
{
    public class ProteinInformation
    {
        public string Name { get; set; }

        public string Accession { get; set; }

        public string Description { get; set; }

        public ProteinInformation(string name, string description, string accession)
        {
            Name = name;
            Accession = accession;
            Description = description;
        }
    }
}
