using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace BiodiversityPlugin.Models
{
    class InsertNewOrganism
    {      
        private static string _keggOrgCode;
        private static string _orgDomain;
        private static string _orgKingdom;
        private static string _orgPhylum;
        private static string _orgClass;
        private static string _orgName;
        private static string _taxon;
        private static List<string> _refseqs = new List<string>();
        private static List<Tuple<string, int>> _peptides = new List<Tuple<string, int>>();
        private static Dictionary<string, KeggGene> _keggGenes = new Dictionary<string, KeggGene>();
        private static Dictionary<string, NcbiProtein> _ncbiProtiens = new Dictionary<string, NcbiProtein>();
        private static Dictionary<string, List<string>> _keggGeneKoMap = new Dictionary<string, List<string>>();
        private static Dictionary<string, List<Tuple<string, int>>> _proteinPeptideMap = new Dictionary<string, List<Tuple<string, int>>>();

        public static void InsertNew(string orgName, string blibLoc, List<string> msgfFolderLoc, string databasePath) //pass in an optional _keggOrgCode?
        {
            FindOrgCode(orgName);
            DownloadKeggGenes();
            DownloadKeggKos();
            DownloadNcbiProteins();
            SearchMsgfFiles(msgfFolderLoc);
            DetermineObserved();
            //InsertIntoDb();
        }

        public static List<string> GetListOfKeggOrganisms()
        {
            var organisms = new List<string>();
            var options = StringSplitOptions.RemoveEmptyEntries;
            var eListUrl = WebRequest.Create("http://rest.kegg.jp/list/organism");
            var listStream = eListUrl.GetResponse().GetResponseStream();
            var lines = new List<string>();
            using (var listReader = new StreamReader(listStream))
            {
                while (listReader.Peek() > -1)
                {
                    var wholeFile = listReader.ReadToEnd();
                    char[] lineSplit = { '\n' };
                    lines = wholeFile.Split(lineSplit, options).ToList();
                    foreach (var line in lines)
                    {
                        organisms.Add((line.Split('\t')[2]).Split('(')[0]);
                    }
                    //Thread.Sleep(1);
                }
            }
            return organisms;
        }

        private static void FindOrgCode(string orgName)
        {
            var options = StringSplitOptions.RemoveEmptyEntries;
            Console.WriteLine(orgName);

            var eListUrl = WebRequest.Create("http://rest.kegg.jp/list/organism");
            var listStream = eListUrl.GetResponse().GetResponseStream();
            var lines = new List<string>();
            using (var listReader = new StreamReader(listStream))
            {
                while (listReader.Peek() > -1)
                {
                    var wholeFile = listReader.ReadToEnd();
                    char[] lineSplit = { '\n' };
                    lines = wholeFile.Split(lineSplit, options).ToList();
                }
            }

            // we know the organism, but need to know the taxon info for it
            foreach (var line in lines)
            {
                if (line.Split('\t')[1] == _keggOrgCode)
                {
                    _keggOrgCode = line.Split('\t')[1];
                    var orgTaxon = line.Split('\t')[3];
                    _orgDomain = orgTaxon.Split(';')[0];
                    _orgKingdom = orgTaxon.Split(';')[1];
                    _orgPhylum = orgTaxon.Split(';')[2];
                    _orgClass = orgTaxon.Split(';')[3];
                    _orgName = line.Split('\t')[2];
                }
            }
        }

        private static void DownloadKeggGenes()
        {
            var options = StringSplitOptions.RemoveEmptyEntries;
            char[] lineSplit = { '\n' };

            var keggGeneUrl = WebRequest.Create("http://rest.kegg.jp/list/" + _keggOrgCode);
            var keggGeneStream = keggGeneUrl.GetResponse().GetResponseStream();
            var lines = new List<string>();
            using (var geneReader = new StreamReader(keggGeneStream))
            {
                while (geneReader.Peek() > -1)
                {
                    var wholeFile = geneReader.ReadToEnd();
                    lines = wholeFile.Split(lineSplit, options).ToList();
                    foreach (var line in lines)
                    {
                        var geneID = line.Split('\t')[0].Split(':')[1];
                        _keggGenes.Add(geneID, new KeggGene(_keggOrgCode, geneID));
                    }
                }
            }
        }

        private static void DownloadKeggKos()
        {
            var keggKoLinkUrl = WebRequest.Create("http://rest.kegg.jp/link/ko/" + _keggOrgCode);
            var keggKoLinkStream = keggKoLinkUrl.GetResponse().GetResponseStream();
            using (var koReader = new StreamReader(keggKoLinkStream))
            {
                while (koReader.Peek() > -1)
                {
                    var line = koReader.ReadLine();
                    var gene = line.Split('\t')[0].Split(':')[1];
                    var ko = line.Split('\t')[1].Split(':')[1];
                    if (!_keggGeneKoMap.ContainsKey(gene))
                    {
                        _keggGeneKoMap.Add(gene, new List<string>());
                    }
                    _keggGeneKoMap[gene].Add(ko);
                    Thread.Sleep(1);
                }
            }
        }

        private static void DownloadNcbiProteins()
        {
            var genes = _keggGenes.Values.ToList();
            var ncbiSettings = new XmlReaderSettings();
            ncbiSettings.DtdProcessing = DtdProcessing.Ignore;
            int giIndex = 0;

            while (giIndex < genes.Count)
            {
                var xmlString = string.Format(
                    "http://eutils.ncbi.nlm.nih.gov/entrez/eutils/efetch.fcgi?db=gene&id={0}&retmode=xml",
                    genes[giIndex].NcbiGeneID);
                var ncbiReader =
                    XmlReader.Create(xmlString, ncbiSettings);
                while (ncbiReader.ReadToFollowing("Gene-track_geneid"))
                {
                    // Gene ID comes first.
                    int geneID = Convert.ToInt32(ncbiReader.ReadElementContentAsString());

                    // Taxon is in this block
                    ncbiReader.ReadToFollowing("Org-ref_db");
                    ncbiReader.ReadToDescendant("Object-id_id");
                    string taxon = ncbiReader.ReadElementContentAsString();

                    // This COULD be the accession we want. Need to check for it to start with YP or NP
                    string accString = "";
                    string versionedAccString = "";
                    string possibleProd = "";
                    while (ncbiReader.ReadToFollowing("Gene-commentary_label"))
                    {
                        possibleProd = ncbiReader.ReadElementContentAsString();
                        ncbiReader.ReadToNextSibling("Gene-commentary_accession");
                        if (ncbiReader.IsStartElement())
                        {
                            var possibleAccession = ncbiReader.ReadElementContentAsString();
                            if (possibleAccession.StartsWith("YP_") || possibleAccession.StartsWith("NP_"))
                            {
                                accString = possibleAccession;
                                ncbiReader.ReadToFollowing("Gene-commentary_version");
                                versionedAccString = accString + "." + ncbiReader.ReadElementContentAsString();
                                break;
                            }
                        }
                    }

                    var prot = new NcbiProtein();

                    prot.RefSeqId = accString;
                    prot.RefSeqIdVersionsed = versionedAccString;
                    prot.taxon = taxon;
                    _taxon = taxon;
                    prot.product = possibleProd;
                    prot.ncbiGeneID = geneID;

                    if (string.IsNullOrWhiteSpace(accString))
                    {
                        Console.WriteLine("Error with Gene ID " + geneID);
                        Console.WriteLine("Blank Accession");
                    }
                    else
                    {
                        _ncbiProtiens.Add(prot.RefSeqId, prot);
                    }
                    giIndex++;
                    if (giIndex % 150 == 0)
                    {
                        Console.WriteLine(giIndex + " out of " + genes.Count);
                    }
                }
            }
        }

        private static void SearchMsgfFiles(List<string> msgfResults)
        {
            double cutoff = 0.0001;
            foreach (var file in msgfResults)
            {
                using (var reader = new StreamReader(file))
                {
                    // read the header in
                    var header = reader.ReadLine().Split('\t');
                    int qValIndex = -1, chargeIndex = -1, pepInd = -1, protInd = -1;
                    for (int i = 0; i < header.Count(); i++)
                    {
                        if (header[i] == "QValue")
                        {
                            qValIndex = i;
                        }
                        if (header[i] == "Charge")
                        {
                            chargeIndex = i;
                        }
                        if (header[i] == "Peptide")
                        {
                            pepInd = i;
                        }
                        if (header[i] == "Protein")
                        {
                            protInd = i;
                        }
                    }
                    if (qValIndex != -1 && chargeIndex != -1 && pepInd != -1 && protInd != -1)
                    {
                        while (reader.Peek() > -1)
                        {
                            var line = reader.ReadLine();
                            var delimiter = '\t';
                            var pieces = line.Split('\t');
                            // qValue (cut off) is in column r (pieces[17])
                            if (Convert.ToDouble(pieces[qValIndex]) < cutoff && pieces[protInd].Split('|').Count() > 1)
                            {
                                var peptide = pieces[pepInd].Split('.')[1];
                                var prot = pieces[protInd].Split('|')[3].Split('.')[0];
                                var charge = Convert.ToInt32(pieces[chargeIndex]);
                                if (!_proteinPeptideMap.ContainsKey(prot))
                                {
                                    _proteinPeptideMap.Add(prot, new List<Tuple<string, int>>());
                                    _refseqs.Add(prot);
                                }
                                if (!_proteinPeptideMap[prot].Contains(new Tuple<string, int>(peptide, charge)))
                                {
                                    _proteinPeptideMap[prot].Add(new Tuple<string, int>(peptide, charge));
                                }
                                if (!_peptides.Contains(new Tuple<string, int>(peptide, charge)))
                                {
                                    _peptides.Add(new Tuple<string, int>(peptide, charge));
                                }
                            }
                        }
                    }
                }
            }
        }

        private static void DetermineObserved()
        {
            foreach (var keggGene in _keggGenes.Values)
            {
                foreach (var refseq in _refseqs)
                {
                    if (_ncbiProtiens.ContainsKey(refseq) && _ncbiProtiens[refseq].ncbiGeneID == keggGene.NcbiGeneID)
                    {
                        keggGene.IsObserved = 1;
                        break;
                    }
                }
            }
        }
    }

    public class KeggGene
    {
        public string KeggOrgCode { get; set; }
        public string KeggGeneID { get; set; }
        public int NcbiGeneID { get; set; }
        public int NcbiGINum { get; set; }
        public int IsObserved { get; set; }
        public List<string> ConnectedPathways { get; set; }

        public KeggGene(string OrgCode, string GeneID)
        {
            KeggOrgCode = OrgCode;
            KeggGeneID = GeneID;
            ConnectedPathways = new List<string>();
            IsObserved = 0;
        }
    }


    internal class NcbiProtein
    {
        public string RefSeqIdVersionsed { get; set; }
        public string RefSeqId { get; set; }
        public int ncbiGeneID { get; set; }
        public string product { get; set; }
        public string taxon { get; set; }
    }
}
