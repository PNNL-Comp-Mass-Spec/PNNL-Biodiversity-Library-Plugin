using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using BiodiversityPlugin.ViewModels;
using KeggParsesClassLibrary;
using MTDBFramework.Algorithms;
using MTDBFramework.Data;
using PNNLOmics;

namespace BiodiversityPlugin.Models
{
    /// <summary>
    /// For replacing an organism, the general path that we follow through code is
    /// 1. identify the organism code at KEGG (given as user input from the 'view' through the method FindOrgCode)
    /// 2. Obtain list of kegg genes and the connected pathways for the specified organism to use in later methods
    /// 3. Search the .blib file that the user provided to find which protein/peptides were observed
    /// 4. Flag which proteins were observed based on the .blib search results
    /// 5. Update the PBL.db and the BlibLoc.db by first resetting all previously observed proteins to "not oberseved" and the updating with the new observed information for each protein
    /// </summary>
    public class UpdateExistingOrganism
    {
        private static Dictionary<string, KeggGene> _keggGenes = new Dictionary<string, KeggGene>();
        private static Dictionary<string, List<Tuple<string, int>>> _proteinPeptideMap = new Dictionary<string, List<Tuple<string, int>>>();
        private static List<string> _uniprots = new List<string>();
        private static List<Tuple<string, int>> _peptides = new List<Tuple<string, int>>();
        private static string _databasePath;
        private static string _orgName;
        private static List<string> _msgfPaths = new List<string>();
        private static string _blibLoc;

        /// <summary>
        /// This method calls all other methods needed to replace organism data. Calling this method will collect, download and
        /// finally update databases with the new information.
        /// </summary>
        /// <param name="orgName"> The name of the organism being updated</param>
        /// <param name="blibLoc">The location of the blib file</param>
        /// <param name="msgfFolderLoc">The location of the PSM results</param>
        /// <param name="databasePath">The location of the current PBL database that contains all organism information</param>
        /// <param name="keggOrgCode">The kegg organism code for the organism being updated</param>
        public static string UpdateExisting(string orgName, string blibLoc, List<string> msgfFolderLoc, string databasePath, string keggOrgCode)
        {
            var reviewResults = "";

            //Do initial clean up of what was in the lists just to be safe
            _keggGenes.Clear();
            _proteinPeptideMap.Clear();
            _uniprots.Clear();
            _peptides.Clear();
            _msgfPaths.Clear();

            //Call all the methods here that will update the existing organism
            _msgfPaths = msgfFolderLoc;
            _blibLoc = blibLoc;
            _orgName = orgName;
            _databasePath = databasePath;
            string orgcode = keggOrgCode;
            GetKeggGenesWithRefs(orgcode);
            GetConnectedPathways(orgcode);
            SearchBlib(blibLoc);
            reviewResults = DetermineObserved();

            return reviewResults;
        }

        /// <summary>
        /// Uses the name of the organism to search the database and find the kegg org code that corresponds to this organism.
        /// </summary>
        /// <param name="orgName">Name of the organism being modified</param>
        /// <returns> The kegg org code</returns>
        public static string GetKeggOrgCode(string orgName, string _databasePath)
        {

            string orgCode = "";
            using (var dbConnection = new SQLiteConnection("Datasource=" + _databasePath + ";Version=3;"))
            {
                dbConnection.Open();
                using (var cmd = new SQLiteCommand(dbConnection))
                {
                    var getOrgText = " SELECT kegg_org_code FROM organism WHERE taxon_name = \"" + orgName + "\" ;"; 
                    cmd.CommandText = getOrgText;
                    cmd.CommandType = CommandType.Text;
                    SQLiteDataReader reader = cmd.ExecuteReader();
                    if (reader.Read())
                    {
                        orgCode = Convert.ToString(reader[0]);
                    }
                }
            }
            return orgCode;
        }

        /// <summary>
        /// This method will query the database to get a list of all the kegg genes which will be used throughout the function to
        /// find and update other information.
        /// </summary>
        /// <param name="keggOrgCode">The kegg org code for this organism</param>
        private static void GetKeggGenesWithRefs(string keggOrgCode)
        {
            using (var dbConnection = new SQLiteConnection("Datasource=" + _databasePath + ";Version=3;"))
            {
                dbConnection.Open();
                using (var cmd = new SQLiteCommand(dbConnection))
                {
                    var getOrgText = "";
                    if (keggOrgCode == "hsa")
                    {
                        getOrgText = " SELECT * from kegg_gene INNER JOIN observed_kegg_gene" +
                                     " ON kegg_gene.kegg_gene_id = observed_kegg_gene.kegg_gene_id" +
                                     " WHERE kegg_gene.kegg_org_code like \"" + keggOrgCode + "%\" " + " GROUP BY kegg_gene.kegg_gene_id;";
                    }
                    else
                    {
                        getOrgText = " SELECT * from kegg_gene INNER JOIN observed_kegg_gene" +
                                     " ON kegg_gene.kegg_gene_id = observed_kegg_gene.kegg_gene_id" +
                                     " WHERE kegg_gene.kegg_org_code = \"" + keggOrgCode + "\" " + " GROUP BY kegg_gene.kegg_gene_id;";
                    }

                    cmd.CommandText = getOrgText;
                    cmd.CommandType = CommandType.Text;
                    SQLiteDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        //Add the kegg gene to dictionary
                        _keggGenes.Add(Convert.ToString(reader["kegg_gene_id"]),
                            new KeggGene(keggOrgCode, Convert.ToString(reader["kegg_gene_id"])));

                        //Pull the uniprots for that organism
                        _keggGenes[Convert.ToString(reader["kegg_gene_id"])].UniprotAcc =
                            Convert.ToString(reader["uniprot"]);
                        
                    }
                }
                dbConnection.Close();
            }                                   
        }

        /// <summary>
        /// Query the database to pull a list of connected pathways and its corresponding gene
        /// </summary>
        /// <param name="keggOrgCode"> The kegg org code for this organism</param>
        private static void GetConnectedPathways(string keggOrgCode)
        {
            using (var dbConnection = new SQLiteConnection("Datasource=" + _databasePath + ";Version=3;"))
            {
                dbConnection.Open();
                using (var cmd = new SQLiteCommand(dbConnection))
                {
                    var getOrgText = "";
                    if (keggOrgCode == "hsa")
                    {
                        getOrgText = " SELECT * FROM observed_kegg_gene WHERE kegg_org_code like \"" + keggOrgCode + "%\" ;";
                    }
                    else
                    {
                        getOrgText = " SELECT * FROM observed_kegg_gene WHERE kegg_org_code = \"" + keggOrgCode + "\" ;";
                    }

                    cmd.CommandText = getOrgText;
                    cmd.CommandType = CommandType.Text;
                    SQLiteDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        if (_keggGenes.ContainsKey(Convert.ToString(reader["kegg_gene_id"])))
                        {
                            _keggGenes[Convert.ToString(reader["kegg_gene_id"])].ConnectedPathways.Add(
                                Convert.ToString(reader["kegg_pathway_id"]));

                            //Set whether the gene has been observed or not
                            _keggGenes[Convert.ToString(reader["kegg_gene_id"])].IsObserved = Convert.ToInt32(reader["is_observed"]);
                        }
                    }
                }
                dbConnection.Close();
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

        /// <summary>
        /// This method will determine which genes have been obeserved and set the IsOberseved value for that gene 
        /// to "1" if it has been observed. It will then display how many proteins have been observed and give
        /// you the option to continue updating or cancelling if the user decides not to proceed.
        /// </summary>
        /// <param name="orgcode">The kegg org code for this organism</param>
        /// <param name="blibLoc">The blib file location for this organism</param>
        /// <param name="orgName"> The name of the organism being updated </param>
        private static string DetermineObserved()
        {
            var reviewResults = "";

            var observedCount = 0;
            var alreadyObserved = 0;
            foreach (var keggGene in _keggGenes.Values)
            {
                if (keggGene.IsObserved == 1)
                {
                    //keggGene was already observed so keep track.
                    alreadyObserved++;
                }
                //Reset the observed variable
                keggGene.IsObserved = 0;

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
            reviewResults = "We parsed " + _blibLoc + " and found " + _peptides.Count + " peptides from " +
                             _uniprots.Count +
                            " proteins for organism " + _orgName + " (" + observedCount + " proteins mapped to KEGG pathways)." + " The plugin currently has " + alreadyObserved +
                            " proteins mapped to KEGG pathways for this organism." +
                            " Your selected option to replace will remove the proteins from the plugin " +
                            "and replace them with the proteins found in your personal data.";
            //"The observed protein count for " + _orgName  + " is " + observedCount + ".";
            return reviewResults;
        }

        /// <summary>
        /// This method will take the newly generated information from the DetermineObserved method and update the database with it
        /// </summary>
        /// <param name="keggOrgCode">The kegg org code for the organism being updated.</param>
        public static void UpdateObservedKeggGeneTable(string keggOrgCode)
        {
            using (var dbConnection = new SQLiteConnection("Datasource=" + _databasePath + ";Version=3;"))
            {
                dbConnection.Open();
                using (var cmd = new SQLiteCommand(dbConnection))
                {
                    var transaction = dbConnection.BeginTransaction();
                    var queryCount = 0;

                    const string obsGeneInsertion = " UPDATE observed_kegg_gene ";
                    foreach (var keggGene in _keggGenes.Values)
                    {
                        foreach (var pathway in keggGene.ConnectedPathways)
                        {
                            var insertion = obsGeneInsertion + 
                                            string.Format(" SET is_observed = " + keggGene.IsObserved + 
                                            " WHERE ( kegg_org_code = {0}{1}{0} AND kegg_pathway_id = {0}{2}{0} AND kegg_gene_id = {0}{3}{0} ); ", '\"',
                                                keggOrgCode, pathway, keggGene.KeggGeneID);
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

        /// <summary>
        /// This method will update the blib location with the new blib provided so it is saved for 
        /// future use if the user runs it again
        /// </summary>
        /// <param name="orgName">Name of the organism being updated</param>
        /// <param name="fileLoc">Location of the blib file</param>
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
