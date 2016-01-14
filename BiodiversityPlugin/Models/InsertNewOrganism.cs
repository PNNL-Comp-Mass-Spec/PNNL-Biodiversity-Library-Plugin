using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        private static List<string> _uniprots = new List<string>();
        private static List<Tuple<string, int>> _peptides = new List<Tuple<string, int>>();
        private static Dictionary<string, Gene> _keggGenes = new Dictionary<string, Gene>();
        private static Dictionary<string, List<string>> _keggGeneKoMap = new Dictionary<string, List<string>>();
        private static Dictionary<string, List<Tuple<string, int>>> _proteinPeptideMap = new Dictionary<string, List<Tuple<string, int>>>();

        public static void InsertNew(string orgName, string blibLoc, List<string> msgfFolderLoc, string databasePath)
        {
            FindOrgCode(orgName);
            _databasePath = databasePath;
            bool go = CheckIfOrgExists(orgName);
            if (!go)
            {
                DownloadKeggGenesAndKos();
                DownloadUniprotIdentifiers();
                DownloadConnectedPathways();
                GetTaxon();
                GetFaaLocation();
                GetProduct();
                SearchMsgfFiles(msgfFolderLoc);
                DetermineObserved(blibLoc, orgName);
            }
            else
            {
                MessageBox.Show(
                    "This organism was already found in the database. Try replacing this organism instead.",
                    "Error Inserting Organism");
            }    
        }

        public static bool CheckIfOrgExists(string orgName)
        {
            bool exists = false;
            var orgCode = "";
            using (var dbConnection = new SQLiteConnection("Datasource=" + _databasePath + ";Version=3;"))
            {
                dbConnection.Open();
                using (var cmd = new SQLiteCommand(dbConnection))
                {
                    var getOrgText = " SELECT kegg_org_code FROM organism WHERE taxon_name = \"" + orgName + "\" ;";
                    cmd.CommandText = getOrgText;
                    SQLiteDataReader reader = cmd.ExecuteReader();
                    if (reader.Read())
                    {
                       orgCode = Convert.ToString(reader[0]);
                    }
                }
            }
            if (orgCode == _keggOrgCode)
            {
                exists = true;
            }
            return exists;
        }

        public static ObservableCollection<OrganismWithFlag> GetListOfKeggOrganisms()
        {
            var organisms = new ObservableCollection<OrganismWithFlag>();
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
                        //Setting custom setting to false since these orgs come from kegg
                        organisms.Add(new OrganismWithFlag(line.Split('\t')[2], false));
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
                    if (_orgDomain == "Prokaryotes" && _orgKingdom == "Bacteria")
                    {
                        _orgDomain = "Bacteria";
                        _orgKingdom = "Eubacteria";
                    }
                    else if (_orgDomain == "Prokaryotes" && _orgKingdom == "Archaea")
                    {
                        _orgDomain = "Archaea";
                    }
                    break;
                }
            }
        }

        private static void DownloadKeggGenesAndKos()
        {
            var options = StringSplitOptions.RemoveEmptyEntries;
            char[] lineSplit = { '\n' };

            var keggGeneUrl = WebRequest.Create("http://rest.kegg.jp/link/ko/" + _keggOrgCode);
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
                        if (!_keggGenes.ContainsKey(geneID))
                        {
                            _keggGenes.Add(geneID, new Gene(_keggOrgCode, geneID));
                        }
                        
                        var koWithPrefix = line.Split('\t')[1];
                        var koID = koWithPrefix.Split(':')[1];

                        //Assign the ko value to that gene
                        _keggGenes[geneID].KeggKOID = koID;

                        //Add the ko to the kegg gene ko map
                        if (!_keggGeneKoMap.ContainsKey(geneID))
                        {
                            _keggGeneKoMap.Add(geneID, new List<string>());
                        }
                        _keggGeneKoMap[geneID].Add(koID);
                    }
                }
            }
        }

        private static void DownloadUniprotIdentifiers()
        {
            var options = StringSplitOptions.RemoveEmptyEntries;
            char[] lineSplit = { '\n' };
            var lines = new List<string>();

            var uniprotListUrl = WebRequest.Create("http://rest.kegg.jp/conv/" + _keggOrgCode + "/uniprot");
            var uniprotListStream = uniprotListUrl.GetResponse().GetResponseStream();
            using (var reader = new StreamReader(uniprotListStream))
            {
                while (reader.Peek() > -1)
                {
                    var wholeFile = reader.ReadToEnd();
                    lines = wholeFile.Split(lineSplit, options).ToList();
                    foreach (var line in lines)
                    {
                        var gene = line.Split('\t')[1].Split(':')[1];
                        var uniprot = line.Split('\t')[0].Split(':')[1];
                        if (_keggGenes.ContainsKey(gene))
                        {
                            _keggGenes[gene].UniprotAcc = uniprot;
                        }
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

        private static void GetFaaLocation()
        {
            _faaLink = KeggInteraction.GetFaaLocation(_taxon);
        }

        private static void GetTaxon()
        {
            var options = StringSplitOptions.RemoveEmptyEntries;
            char[] lineSplit = { '\n' };
            var lines = new List<string>();

            var taxonUrl = WebRequest.Create("http://www.kegg.jp/kegg-bin/show_organism?org=" + _keggOrgCode);
            var taxonStream = taxonUrl.GetResponse().GetResponseStream();
            using (var reader = new StreamReader(taxonStream))
            {
                while (reader.Peek() > -1)
                {
                    var wholeFile = reader.ReadToEnd();
                    lines = wholeFile.Split(lineSplit, options).ToList();

                    foreach (var line in lines)
                    {
                        if (line.Contains("TAX:"))
                        {
                            _taxon = line.Split('>')[7].Split('<')[0];
                        }
                    }
                }
            }
        }

        private static void GetProduct()
        {
            var options = StringSplitOptions.RemoveEmptyEntries;
            char[] lineSplit = { '\n' };
            var lines = new List<string>();

            var GeneListUrl = WebRequest.Create("http://rest.kegg.jp/list/" + _keggOrgCode);
            var GeneListStream = GeneListUrl.GetResponse().GetResponseStream();
            using (var geneReader = new StreamReader(GeneListStream))
            {
                while (geneReader.Peek() > -1)
                {
                    var wholeFile = geneReader.ReadToEnd();
                    lines = wholeFile.Split(lineSplit, options).ToList();
                    foreach (var line in lines)
                    {
                        var geneID = line.Split('\t')[0].Split(':')[1];
                        var product = line.Split('\t')[1];
                        if (_keggGenes.ContainsKey(geneID))
                        {
                            _keggGenes[geneID].product = product;
                        }                     
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
                            var pieces = line.Split('\t');
                            // qValue (cut off) is in column r (pieces[17])
                            if (Convert.ToDouble(pieces[qValIndex]) < cutoff && !string.IsNullOrEmpty(pieces[protInd]))
                            {
                                var peptide = pieces[pepInd].Split('.')[1];

                                var prot = pieces[protInd]; //Getting the uniprot identifier

                                var charge = Convert.ToInt32(pieces[chargeIndex]);

                                if (!_proteinPeptideMap.ContainsKey(prot))
                                {
                                    _proteinPeptideMap.Add(prot, new List<Tuple<string, int>>());
                                    _uniprots.Add(prot);
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
                    foreach (var uniprot in _uniprots)
                    {
                        if (uniprot == keggGene.UniprotAcc)
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
                _uniprots.Clear();
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
                            " INSERT INTO organism_taxonomy (taxon_id, og_domain, og_kingdom, og_phylum, og_class, og_species) VALUES ( {0}{1}{0},{0}{2}{0},{0}{3}{0},{0}{4}{0},{0}{5}{0},{0}{6}{0} ); ",
                            '\"', _taxon, _orgDomain, _orgKingdom, _orgPhylum, _orgClass, _orgName);
                    cmd.CommandText = orgInsertion;
                    cmd.ExecuteNonQuery();
                    queryCount++;

                    string faaLocationInsertion =
                        string.Format(" INSERT INTO orgFaaLocation (kegg_org_code, org_faa_location) " +
                                      "VALUES ({0}{1}{0}, {0}{2}{0});", '\"', _keggOrgCode, _faaLink);
                    cmd.CommandText = faaLocationInsertion;
                    cmd.ExecuteNonQuery();
                    queryCount++;

                    const string geneInsertion = " INSERT INTO kegg_gene (kegg_org_code, kegg_gene_id, uniprot, product) ";
                    //cmd.CommandText = geneInsertion;
                    foreach (var keggGene in _keggGenes.Values)
                    {
                            string insertion;
                            if (!string.IsNullOrEmpty(keggGene.UniprotAcc))
                            {
                                insertion = geneInsertion +
                                    string.Format(" VALUES ( {0}{1}{0},{0}{2}{0},{0}{3}{0},{0}{4}{0} ); ", '\"', _keggOrgCode,
                                        keggGene.KeggGeneID, keggGene.UniprotAcc, keggGene.product);
                            }
                            else
                            {
                                //Missing identifier so do not enter into the database.
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
        public string UniprotAcc { get; set; }
        public string KeggKOID { get; set; }
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
            UniprotAcc = "";
            KeggKOID = "";
            product = "";
        }
    }
}
