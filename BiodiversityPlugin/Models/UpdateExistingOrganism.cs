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
using MessageBox = System.Windows.MessageBox;

namespace BiodiversityPlugin.Models
{
    /// <summary>
    /// Contains all methods needed to replace the current organism data with different PSM results.
    /// </summary>
    public class UpdateExistingOrganism
    {
        private static Dictionary<string, KeggGene> _keggGenes = new Dictionary<string, KeggGene>();
        private static Dictionary<string, List<Tuple<string, int>>> _proteinPeptideMap = new Dictionary<string, List<Tuple<string, int>>>();
        private static List<string> _uniprots = new List<string>();
        private static List<Tuple<string, int>> _peptides = new List<Tuple<string, int>>();
        private static string _databasePath;
        private static string _orgName;

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

            //Call all the methods here that will update the existing organism
            _orgName = orgName;
            _databasePath = databasePath;
            string orgcode = keggOrgCode;
            GetKeggGenesWithRefs(orgcode);
            GetConnectedPathways(orgcode);
            SearchMsgfFiles(msgfFolderLoc);
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
                    var getOrgText = " SELECT * from kegg_gene INNER JOIN observed_kegg_gene" +
                                     " ON kegg_gene.kegg_gene_id = observed_kegg_gene.kegg_gene_id" +
                                     " WHERE kegg_gene.kegg_org_code = \"" + keggOrgCode + "\" " + " GROUP BY kegg_gene.kegg_gene_id;";
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
                    var getOrgText = " SELECT * FROM observed_kegg_gene WHERE kegg_org_code = \"" + keggOrgCode + "\" ;";
                    cmd.CommandText = getOrgText;
                    cmd.CommandType = CommandType.Text;
                    SQLiteDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        if (_keggGenes.ContainsKey(Convert.ToString(reader["kegg_gene_id"])))
                        {
                            _keggGenes[Convert.ToString(reader["kegg_gene_id"])].ConnectedPathways.Add(
                                Convert.ToString(reader["kegg_pathway_id"]));
                        }
                    }
                }
                dbConnection.Close();
            }
        }

        /// <summary>
        /// This method will search the PSM results file for this organism
        /// </summary>
        /// <param name="msgfFolder">Location of the PSM results</param>
        private static void SearchMsgfFiles(List<string> msgfFolder)
        {
            double cutoff = 0.0001;

            foreach (var file in msgfFolder)
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

                                var prot = pieces[protInd];

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
            foreach (var keggGene in _keggGenes.Values)
            {
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
            reviewResults = "The observed protein count for " + _orgName  + " is " + observedCount + ". To confirm these changes, press Finish. To cancel, press Cancel.";
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
            } 
        }
    }
} 
