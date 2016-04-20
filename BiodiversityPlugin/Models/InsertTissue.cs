using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using KeggParsesClassLibrary;

namespace BiodiversityPlugin.Models
{
    public class InsertTissue
    {
        private static string _keggOrgCode;
        private static string _orgDomain = "Eukaryota";
        private static string _orgKingdom = "";
        private static string _orgPhylum = "Animalia";
        private static string _orgClass = "Homo sapiens";
        private static string _parentOrgCode = "hsa";
        private static string _orgName;
        private static string _parentTaxon = "9606";
        private static string _taxon;
        private static string _databasePath;
        private static string _faaLink;
        private static List<string> _uniprots = new List<string>();
        private static List<Tuple<string, int>> _peptides = new List<Tuple<string, int>>();
        private static Dictionary<string, Gene> _keggGenes = new Dictionary<string, Gene>();
        private static Dictionary<string, List<string>> _keggGeneKoMap = new Dictionary<string, List<string>>();
        private static Dictionary<string, List<Tuple<string, int>>> _proteinPeptideMap = new Dictionary<string, List<Tuple<string, int>>>();
        private static List<string> _msgfPaths = new List<string>();
        private static string _blibLoc;

        public static string InsertNew(string orgName, string orgCode, string orgTaxon, string blibLoc, List<string> msgfFolderLoc, string databasePath, out bool alreadyAdded)
        {
            var reviewResults = "";

            //Initialize by clearing everything out first
            _keggGenes.Clear();
            _keggGeneKoMap.Clear();
            _proteinPeptideMap.Clear();
            _uniprots.Clear();
            _peptides.Clear();
            _msgfPaths.Clear();

            _msgfPaths = msgfFolderLoc;
            _blibLoc = blibLoc;
            _keggOrgCode = orgCode;
            _orgName = orgName;
            _databasePath = databasePath;
            bool go = CheckIfOrgExists(orgName);
            if (!go)
            {
                alreadyAdded = false;
                DownloadKeggGenesAndKos();
                DownloadUniprotIdentifiers();
                DownloadConnectedPathways();
                _taxon = orgTaxon;
                GetFaaLocation();
                GetProduct();
                SearchBlib(blibLoc);
                reviewResults = DetermineObserved();
            }
            else
            {
                reviewResults =
                    orgName + " was already found in the database. Try replacing or renaming this organism instead.";
                alreadyAdded = true;
            }
            return reviewResults;
        }

        /// <summary>
        /// Method to check if the org that the user is trying to add already exists in
        /// the database already
        /// </summary>
        /// <param name="orgName"> Name of the organism to search for</param>
        /// <returns></returns>
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

        private static void DownloadKeggGenesAndKos()
        {
            var options = StringSplitOptions.RemoveEmptyEntries;
            char[] lineSplit = { '\n' };

            var keggGeneUrl = WebRequest.Create("http://rest.kegg.jp/link/ko/" + _parentOrgCode);
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

            var uniprotListUrl = WebRequest.Create("http://rest.kegg.jp/conv/" + _parentOrgCode + "/uniprot");
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

            var PathwayGeneListUrl = WebRequest.Create("http://rest.kegg.jp/link/pathway/" + _parentOrgCode);
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
                        if (_keggGenes.ContainsKey(geneID))
                        {
                            _keggGenes[geneID].ConnectedPathways.Add(pathPiece.Substring(pathPiece.Length - 5));
                        }

                    }
                }
            }
        }

        /// <summary>
        /// Method to get the fasta location from the uniprot website
        /// </summary>
        private static void GetFaaLocation()
        {
            _faaLink = KeggInteraction.GetFaaLocation(_parentTaxon);
        }

        private static void GetProduct()
        {
            var options = StringSplitOptions.RemoveEmptyEntries;
            char[] lineSplit = { '\n' };
            var lines = new List<string>();

            var GeneListUrl = WebRequest.Create("http://rest.kegg.jp/list/" + _parentOrgCode);
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

        private static void SearchBlib(string blibLoc)
        {
            var listOfProteinsToConvert = new List<Tuple<string, string, int>>();
            var converted = new List<Tuple<string, string, int>>();

            using (var dbConnection = new SQLiteConnection("Datasource=" + blibLoc + ";Version=3"))
            {
                dbConnection.Open();
                using (var cmd = new SQLiteCommand(dbConnection))
                {
                    var select = "SELECT Proteins.accession, RefSpectra.peptideSeq, RefSpectra.precursorCharge FROM RefSpectraProteins" +
                                " INNER JOIN Proteins" +
                                " ON RefSpectraProteins.ProteinId = Proteins.id" +
                                " INNER JOIN RefSpectra" +
                                " ON RefSpectraProteins.RefSpectraId = RefSpectra.id; ";
                    cmd.CommandText = select;
                    cmd.CommandType = CommandType.Text;
                    SQLiteDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        bool alreadyUniprot = false;
                        var protein = Convert.ToString(reader["accession"]);
                        if (protein.Contains("Contaminant"))
                        {
                            continue;
                        }
                        if (protein.Contains('|'))
                        {
                            protein = protein.Split('|')[1];
                            alreadyUniprot = true;
                        }
                        var peptide = Convert.ToString(reader["peptideSeq"]);
                        var charge = Convert.ToInt32(reader["precursorCharge"]);

                        if (alreadyUniprot)
                        {
                            converted.Add(new Tuple<string, string, int>(protein, peptide, charge));
                        }
                        else
                        {
                            listOfProteinsToConvert.Add(new Tuple<string, string, int>(protein, peptide, charge));
                        }
                    }
                }
                dbConnection.Close();
            }

            var listOfQueryStrings = new List<string>();

            var queryCount = 0;
            string queryUniprot = "http://www.uniprot.org/uniprot/?query=";
            foreach (var protein in listOfProteinsToConvert)
            {
                queryUniprot = queryUniprot + protein.Item1 + "+or+";
                queryCount++;
                if (queryCount == 50)
                {
                    queryUniprot = queryUniprot.Substring(0, (queryUniprot.Length - 4));
                    listOfQueryStrings.Add(queryUniprot);
                    //Reset values and start over
                    queryCount = 0;
                    queryUniprot = "http://www.uniprot.org/uniprot/?query=";
                }
            }
            //Add final time just in case it didn't reach 50
            queryUniprot = queryUniprot.Substring(0, (queryUniprot.Length - 4));
            listOfQueryStrings.Add(queryUniprot);

            //Send list of queries to get queried in uniprot
            var dictOfProteinToUniprots = ConvertToUniprot(listOfQueryStrings);

            foreach (var protein in listOfProteinsToConvert)
            {
                if (dictOfProteinToUniprots.ContainsKey(protein.Item1))
                {
                    var uniprot = dictOfProteinToUniprots[protein.Item1];
                    converted.Add(new Tuple<string, string, int>(uniprot, protein.Item2, protein.Item3));
                }
            }

            foreach (var protein in converted)
            {
                if (string.IsNullOrEmpty(protein.Item1))
                {
                    //If it returns null then that uniprot no longer exists so skip over
                    continue;
                }

                //Add 
                if (!_proteinPeptideMap.ContainsKey(protein.Item1))
                {
                    _proteinPeptideMap.Add(protein.Item1, new List<Tuple<string, int>>());
                    _uniprots.Add(protein.Item1);
                }
                if (!_proteinPeptideMap[protein.Item1].Contains(new Tuple<string, int>(protein.Item2, protein.Item3)))
                {
                    _proteinPeptideMap[protein.Item1].Add(new Tuple<string, int>(protein.Item2, protein.Item3));
                }
                if (!_peptides.Contains(new Tuple<string, int>(protein.Item2, protein.Item3)))
                {
                    _peptides.Add(new Tuple<string, int>(protein.Item2, protein.Item3));
                }
            }
        }

        private static Dictionary<string, string> ConvertToUniprot(List<string> urlPaths)
        {
            Dictionary<string, string> uniprotToProtein = new Dictionary<string, string>();
            foreach (var urlPath in urlPaths)
            {
                var url = WebRequest.Create(urlPath + "&format=FASTA&columns=id");
                var urlStream = url.GetResponse().GetResponseStream();
                using (var reader = new StreamReader(urlStream))
                {
                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();
                        if (line != null && line.Contains('|') && line.StartsWith(">"))
                        {
                            var uniprot = line.Split('|')[1];
                            var protein = line.Split('|')[2].Split(' ')[0];
                            if (!uniprotToProtein.ContainsKey(protein))
                            {
                                uniprotToProtein.Add(protein, uniprot);
                            }
                        }
                    }
                    reader.Close();
                }
            }
            return uniprotToProtein;
        }

        private static string DetermineObserved()
        {
            var reviewResults = "";
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

            reviewResults = "We parsed " + _blibLoc + " and found " + _peptides.Count +
                            " peptides from "
                            + _uniprots.Count + " proteins for Human tissue " + _orgName + " ("
                            + observedCount + " proteins mapped to KEGG pathways).";
            //"The observed protein count for " + _orgName + " is " + observedCount + ".";
            return reviewResults;
        }

        public static void InsertIntoDb()
        {
            using (var dbConnection = new SQLiteConnection("Datasource=" + _databasePath + ";Version=3;"))
            {
                dbConnection.Open();
                using (var cmd = new SQLiteCommand(dbConnection))
                {
                   var transaction = dbConnection.BeginTransaction();

                    // Read the metadata table to determine the number of human tissues in there, then increment by one (And increase version number by 1)
                    const string MetaDataQuery =
                        " SELECT * FROM MetaData ORDER BY db_version_date DESC, db_version_revision DESC, db_version_minor DESC, db_version_major DESC LIMIT 1; ";
                    cmd.CommandText = MetaDataQuery;
                    var _numHumanTissues = 0;
                    int[] version = { -1, -1, -1 };
                    using (var reader = cmd.ExecuteReader())
                    {
                        var dbRev = 0;
                        bool validNum = true;
                        while (reader.Read())
                        {
                            var major = reader.GetInt32(0);
                            var minor = reader.GetInt32(1);
                            dbRev = reader.GetInt32(2);
                            _numHumanTissues = reader.GetInt32(4);

                            version[0] = major;
                            version[1] = minor;
                            version[2] = dbRev + 1;
                            _numHumanTissues++;
                        }
                    }

                    string MetaDataInsertion = string.Format(" INSERT INTO MetaData VALUES ( {0},{1},{2},\"{3}\",{4} )",
                        version[0], version[1], version[2], DateTime.Now.ToString("yyyy-MM-dd"), _numHumanTissues);

                    cmd.CommandText = MetaDataInsertion;
                    cmd.ExecuteNonQuery();
                    transaction.Commit();

                    transaction = dbConnection.BeginTransaction();

                    var queryCount = 0;

                    string orgInsertion =
                        string.Format(" INSERT INTO organism VALUES ( \"{1}\",{0}{2}{0},{0}{3}{0},{0}{4}{0} ); ", '\"', _taxon,
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
                dbConnection.Close();
            }
        }

        public static void UpdateBlibLocation(string orgName, string fileLoc)
        {
            var fileLocSource = _databasePath.Replace("PBL.db", "..//blibFileLoc.db");

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
            }
        }
    }

}
