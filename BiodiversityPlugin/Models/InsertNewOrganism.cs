using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;
using KeggParsesClassLibrary;

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
        private static string _databasePath;
        private static string _faaLink;
        private static List<string> _refseqs = new List<string>();
        private static List<Tuple<string, int>> _peptides = new List<Tuple<string, int>>();
        private static Dictionary<string, string> giRefs = new Dictionary<string, string>();
        private static Dictionary<string, Gene> _keggGenes = new Dictionary<string, Gene>();
        private static Dictionary<string, List<string>> _keggGeneKoMap = new Dictionary<string, List<string>>();
        private static Dictionary<string, List<Tuple<string, int>>> _proteinPeptideMap = new Dictionary<string, List<Tuple<string, int>>>();

        public static void InsertNew(string orgName, string blibLoc, List<string> msgfFolderLoc, string databasePath) 
        {
            _databasePath = databasePath;
            FindOrgCode(orgName);
            DownloadKeggGenes();
            DownloadNcbiGeneIds();
            DownloadNcbiGiNums();
            DownloadConnectedPathways();
            DownloadKeggKos();
            GetFaaLocation();
            GetGiRefDictionary();
            DownloadRefseqs();
            GetTaxonAndProduct();
            SearchMsgfFiles(msgfFolderLoc);
            DetermineObserved(blibLoc, orgName);
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
                if (line.Split('\t')[2] == orgName)
                {
                    _keggOrgCode = line.Split('\t')[1];
                    var orgTaxon = line.Split('\t')[3];
                    _orgDomain = orgTaxon.Split(';')[0];
                    _orgKingdom = orgTaxon.Split(';')[1];
                    _orgPhylum = orgTaxon.Split(';')[2];
                    _orgClass = orgTaxon.Split(';')[3];
                    _orgName = line.Split('\t')[2];
                    break;
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
                        _keggGenes.Add(geneID, new Gene(_keggOrgCode, geneID));
                    }
                }
            }
        }

        private static void DownloadNcbiGeneIds()
        {
            var options = StringSplitOptions.RemoveEmptyEntries;
            char[] lineSplit = { '\n' };
            var lines = new List<string>();

            var ncbiGeneIdUrl = WebRequest.Create("http://rest.kegg.jp/conv/ncbi-geneid/" + _keggOrgCode);
            var ncbiGeneIdStream = ncbiGeneIdUrl.GetResponse().GetResponseStream();
            using (var geneReader = new StreamReader(ncbiGeneIdStream))
            {
                while (geneReader.Peek() > -1)
                {
                    var wholeFile = geneReader.ReadToEnd();
                    lines = wholeFile.Split(lineSplit, options).ToList();
                    foreach (var line in lines)
                    {
                        var geneID = line.Split('\t')[0].Split(':')[1];
                        _keggGenes[geneID].NcbiGeneID = Convert.ToInt32(line.Split('\t')[1].Split(':')[1]);
                    }
                }
            }
        }

        private static void DownloadNcbiGiNums()
        {
            var options = StringSplitOptions.RemoveEmptyEntries;
            char[] lineSplit = { '\n' };
            var lines = new List<string>();

            var ncbiGiNumUrl = WebRequest.Create("http://rest.kegg.jp/conv/ncbi-gi/" + _keggOrgCode);
            var ncbiGiNumStream = ncbiGiNumUrl.GetResponse().GetResponseStream();
            using (var geneReader = new StreamReader(ncbiGiNumStream))
            {
                while (geneReader.Peek() > -1)
                {
                    var wholeFile = geneReader.ReadToEnd();
                    lines = wholeFile.Split(lineSplit, options).ToList();
                    foreach (var line in lines)
                    {
                        var geneID = line.Split('\t')[0].Split(':')[1];
                        _keggGenes[geneID].NcbiGINum = line.Split('\t')[1].Split(':')[1];
                    }
                }
            }
        }

        private static void DownloadConnectedPathways()
        {
            var options = StringSplitOptions.RemoveEmptyEntries;
            char[] lineSplit = { '\n' };
            var lines = new List<string>();

            var PathwayGeneListUrl = WebRequest.Create("http://rest.kegg.jp/link/pathway/" + _keggOrgCode);
            var PathwayGeneListStream = PathwayGeneListUrl.GetResponse().GetResponseStream();
            using (var geneReader = new StreamReader(PathwayGeneListStream))
            {
                while (geneReader.Peek() > -1)
                {
                    var wholeFile = geneReader.ReadToEnd();
                    lines = wholeFile.Split(lineSplit, options).ToList();
                    foreach (var line in lines)
                    {
                        var geneID = line.Split('\t')[0].Split(':')[1];
                        var pathPiece = line.Split('\t')[1];
                        _keggGenes[geneID].ConnectedPathways.Add(pathPiece.Substring(pathPiece.Length - 5));
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
                    _keggGenes[gene].KeggKOID = ko;
                    Thread.Sleep(1);
                }
            }
        }

        private static void GetFaaLocation()
        {
            _faaLink = MismatchedRefseqFinder.ParseAndGetOneLink(_keggOrgCode);
        }

        private static void GetGiRefDictionary()
        {
            FastaDownloaderNCBI.Program.Main(new string[] { _faaLink });
            foreach (var file in Directory.GetFiles(@"."))
            {
                if (file.EndsWith(".fasta"))
                {
                    using (var reader = new StreamReader(file))
                    {
                        while (reader.Peek() > -1)
                        {
                            var line = reader.ReadLine();
                            if (line.StartsWith(">"))
                            {
                                var gi = line.Split('|')[2].Split(')')[0];
                                var refseq = line.Split('|')[1].Split(' ')[0];
                                try
                                {
                                    //If the gi and refseq have not been seen yet, add to dictionary
                                    giRefs.Add(gi, refseq);

                                }
                                catch (Exception)
                                {
                                    //Let user know that the gi has already been seen, skip over and don't add
                                    Console.WriteLine("Already seen this gi: " + gi + " ref: " + refseq);
                                    Console.WriteLine("Dictionary contains " + giRefs[gi] + " at above gi num");
                                }
                            }
                        }
                    }
                    File.Delete(file);
                }
            }
        }

        private static void DownloadRefseqs()
        {
            foreach (var keggGene in _keggGenes.Values)
            {
                if (giRefs.ContainsKey(keggGene.NcbiGINum))
                {
                    keggGene.RefSeqIDVersioned = giRefs[keggGene.NcbiGINum];
                    keggGene.RefseqID = keggGene.RefSeqIDVersioned.Split('.').First();
                }
            }
        }

        private static void GetTaxonAndProduct()
        {
            int giIndex = 0;
            var ncbiSettings = new XmlReaderSettings();
            ncbiSettings.DtdProcessing = DtdProcessing.Ignore;

            foreach (var gene in _keggGenes.Values)
            {
                var xmlString = string.Format(
                    "http://eutils.ncbi.nlm.nih.gov/entrez/eutils/efetch.fcgi?db=gene&id={0}&retmode=xml",
                    gene.NcbiGeneID);
                var ncbiReader =
                    XmlReader.Create(xmlString, ncbiSettings);
                while (ncbiReader.ReadToFollowing("Gene-track_geneid"))
                {
                    //Pull taxon
                    ncbiReader.ReadToFollowing("Org-ref_db");
                    ncbiReader.ReadToDescendant("Dbtag_db");
                    // While what ever is in ^^ is not "taxon", keep looking for the next dbtag_db element.
                    while (ncbiReader.ReadElementContentAsString() != "taxon")
                    {
                        ncbiReader.ReadToFollowing("Dbtag_db");
                    }
                    ncbiReader.ReadToFollowing("Object-id_id");
                    string taxon = ncbiReader.ReadElementContentAsString();
                    _taxon = taxon;

                    //Pull product
                    try
                    {
                        ncbiReader.ReadToFollowing("Prot-ref_name_E");
                        gene.product = ncbiReader.ReadElementContentAsString();
                    }
                    catch (Exception ex)
                    {
                        gene.product = "";
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

        private static void DetermineObserved(string blibLoc, string orgName)
        {
            var observedCount = 0;
            foreach (var keggGene in _keggGenes.Values)
            {
                keggGene.IsObserved = 0;
                if (!string.IsNullOrWhiteSpace(keggGene.KeggKOID) && keggGene.ConnectedPathways.Count > 0)
                {
                    foreach (var refseq in _refseqs)
                    {
                        if (refseq.Split('.').First() == keggGene.RefseqID)
                        {
                            keggGene.IsObserved = 1;
                            observedCount++;
                            break;
                        }
                    }
                }               
            }
            var result = MessageBox.Show("The observed protein count is " + observedCount + ". Would you like to continue? ", "Search Complete",
                MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                InsertIntoDb();
                UpdateBlibLocation(orgName, blibLoc);
            }
            if (result == MessageBoxResult.No)
            {
                //Clear variable here so they can go back and choose a differnet file
                _keggGenes.Clear();
                _proteinPeptideMap.Clear();
                _refseqs.Clear();
                _peptides.Clear();
            }
        }

        private static void InsertIntoDb()
        {
            using (var dbConnection = new SQLiteConnection("Datasource=" + _databasePath + ";Version=3;"))
            {
                dbConnection.Open();
                using (var cmd = new SQLiteCommand(dbConnection))
                {
                    var transaction = dbConnection.BeginTransaction();
                    var queryCount = 0;

                    string orgInsertion =
                        string.Format(" INSERT INTO organism VALUES ( {1},{0}{2}{0},{0}{3}{0},{0}{4}{0} ); ", '\"', _taxon,
                            _orgName, _keggOrgCode, _orgName);
                    cmd.CommandText = orgInsertion;
                    cmd.ExecuteNonQuery();
                    queryCount++;

                    orgInsertion =
                        string.Format(
                            " INSERT INTO organism_taxonomy (ncbi_taxon_id, og_domain, og_kingdom, og_phylum, og_class, og_species) VALUES ( {0}{1}{0},{0}{2}{0},{0}{3}{0},{0}{4}{0},{0}{5}{0},{0}{6}{0} ); ",
                            '\"', _taxon, _orgDomain, _orgKingdom, _orgPhylum, _orgClass, _orgName);
                    cmd.CommandText = orgInsertion;
                    cmd.ExecuteNonQuery();
                    queryCount++;

                    string faaLocationInsertion =
                        string.Format(" INSERT INTO orgFaaLocation (kegg_org_code, ncbi_org_location) " +
                                      "VALUES ({0}{1}{0}, {0}{2}{0});", '\"', _keggOrgCode, _faaLink);
                    cmd.CommandText = faaLocationInsertion;
                    cmd.ExecuteNonQuery();
                    queryCount++;

                    const string geneInsertion = " INSERT INTO kegg_gene ";
                    //cmd.CommandText = geneInsertion;
                    foreach (var keggGene in _keggGenes.Values)
                    {
                        if (!string.IsNullOrEmpty(keggGene.KeggKOID))
                        {
                            string insertion;
                            if (!string.IsNullOrEmpty(keggGene.RefseqID))
                            {
                                insertion = geneInsertion +
                                    string.Format(" VALUES ( {0}{1}{0},{0}{2}{0},{0}{3}{0},{0}{4}{0},{0}{5}{0},{0}{6}{0} ); ", '\"', _keggOrgCode,
                                        keggGene.KeggGeneID, keggGene.RefseqID, keggGene.RefSeqIDVersioned, keggGene.UniprotAcc, keggGene.product);
                            }
                            else
                            {
                                //Missing refseq so do not enter into the database.
                                continue;
                            }
                            cmd.CommandText = insertion;
                            cmd.ExecuteNonQuery();
                            queryCount++;
                            if (queryCount % 10000 == 0)
                            {
                                transaction.Commit();
                                transaction = dbConnection.BeginTransaction();
                            }
                        }
                    }

                    const string obsGeneInsertion = " INSERT INTO observed_kegg_gene ";
                    cmd.CommandText = geneInsertion;
                    foreach (var keggGene in _keggGenes.Values)
                    {
                        foreach (var pathway in keggGene.ConnectedPathways)
                        {
                            var insertion = obsGeneInsertion +
                                            string.Format(" VALUES ( {0}{1}{0},{0}{2}{0},{0}{3}{0},{4} ); ", '\"',
                                                _keggOrgCode,
                                                pathway, keggGene.KeggGeneID, keggGene.IsObserved);
                            cmd.CommandText = insertion;
                            cmd.ExecuteNonQuery();
                            queryCount++;
                            if (queryCount % 10000 == 0)
                            {
                                transaction.Commit();
                                transaction = dbConnection.BeginTransaction();
                            }
                        }
                    }

                    const string geneKoInsertion = " INSERT INTO kegg_gene_ko_map ";
                    foreach (var pair in _keggGeneKoMap)
                    {
                        foreach (var ortholog in pair.Value)
                        {
                            var insertion = geneKoInsertion +
                                            string.Format(" VALUES ( {0}{1}{0},{0}{2}{0},{0}{3}{0} ); ", '\"', _keggOrgCode, pair.Key,
                                                ortholog);
                            cmd.CommandText = insertion;
                            cmd.ExecuteNonQuery();
                            queryCount++;
                            if (queryCount % 10000 == 0)
                            {
                                transaction.Commit();
                                transaction = dbConnection.BeginTransaction();
                            }
                        }
                    }
                    transaction.Commit();
                }
            }
        }

        private static void UpdateBlibLocation(string orgName, string fileLoc)
        {
            var fileLocSource = _databasePath.Replace("PBL.db", "blibFileLoc.db");

            using (var dbConnection = new SQLiteConnection("Datasource=" + fileLocSource + ";Version=3;"))
            {
                dbConnection.Open();
                using (var cmd = new SQLiteCommand(dbConnection))
                {
                    var insertUpdate = " INSERT or REPLACE INTO fileLocation (orgName, fileLocation, custom)";
                    var lastBit = string.Format(" VALUES ({0}{1}{0}, {0}{2}{0}, {0}{3}{0}); ", "\"", orgName, fileLoc, true);
                    cmd.CommandText = insertUpdate + lastBit;
                    cmd.ExecuteNonQuery();

                    var insertType = "INSERT or REPLACE INTO customOrganisms (orgName, bothBlibs)";
                    var insertLast = string.Format(" VALUES ({0}{1}{0}, {0}{2}{0}); ", "\"", orgName, false);
                    cmd.CommandText = insertType + insertLast;
                    cmd.ExecuteNonQuery();
                }
                dbConnection.Close();
                MessageBox.Show("Organism and blib file location have been updated successfully.", "Finished!");

            }
        }
    }

    class Gene
    {
        public string KeggOrgCode { get; set; }
        public string KeggGeneID { get; set; }
        public int NcbiGeneID { get; set; }
        public string NcbiGINum { get; set; }
        public string RefseqID { get; set; }
        public string RefSeqIDVersioned { get; set; }
        public string UniprotAcc { get; set; }
        public string KeggKOID { get; set; }
        public string KeggDesc { get; set; }
        public string product { get; set; }
        public List<string> ConnectedPathways { get; set; }
        public int IsObserved { get; set; }

        /// <summary>
        /// This is the constructor for this class. It sets all the values as a blank as default.
        /// </summary>
        /// <param name="OrgCode"></param>
        /// <param name="GeneID"></param>
        public Gene(string OrgCode, string GeneID)
        {
            KeggOrgCode = OrgCode;
            KeggGeneID = GeneID;
            ConnectedPathways = new List<string>();
            IsObserved = 0;
            NcbiGeneID = 0;
            NcbiGINum = "";
            RefseqID = "";
            UniprotAcc = "";
            KeggKOID = "";
            KeggDesc = "";
            product = "";
            RefSeqIDVersioned = "";
        }
    }
}
