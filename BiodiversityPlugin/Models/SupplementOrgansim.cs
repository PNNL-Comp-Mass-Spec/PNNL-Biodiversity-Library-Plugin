﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using BiodiversityPlugin.ViewModels;
using KeggParsesClassLibrary;

namespace BiodiversityPlugin.Models
{
    class SupplementOrgansim
    {
        private static Dictionary<string, KeggGene> _keggGenes = new Dictionary<string, KeggGene>();
        private static Dictionary<string, List<Tuple<string, int>>> _proteinPeptideMap = new Dictionary<string, List<Tuple<string, int>>>();
        private static List<string> _uniprots = new List<string>();
        private static List<Tuple<string, int>> _peptides = new List<Tuple<string, int>>();
        private static string _databasePath;
        private static string _orgName;
        private static List<string> _msgfPaths = new List<string>();

        /// <summary>
        /// Starting entry point of the supplement an organism's data action
        /// </summary>
        /// <param name="orgName">Name of the organism to be customized</param>
        /// <param name="blibLoc">Path to the blib file for this organism</param>
        /// <param name="msgfFolderLoc">Path to the msgf results location for this organism</param>
        /// <param name="databasePath">Path to the database the be worked on (its all internal but I need a way to reach it, therefore passing it in</param>
        /// <param name="keggOrgCode">The KEGG organism code for this organism</param>
        /// <returns></returns>
        public static string Supplement(string orgName, string blibLoc, List<string> msgfFolderLoc, string databasePath, string keggOrgCode)
        {
            //String message to return back to the view model to display to the user. 
            //Includes information about what proteins were observed
            var reviewResults = "";

            //Do initial clean up of what was in the lists just to be safe
            _keggGenes.Clear();
            _proteinPeptideMap.Clear();
            _uniprots.Clear();
            _peptides.Clear();
            _msgfPaths.Clear();

            //Set variables
            _msgfPaths = msgfFolderLoc;
            _orgName = orgName;
            _databasePath = databasePath;
            string orgcode = keggOrgCode;

            //Begin customizing
            GetKeggGenesWithRefs(orgcode);
            GetConnectedPathways(orgcode);
            SearchMsgfFiles(msgfFolderLoc);
            reviewResults = DetermineObserved();

            return reviewResults;
        }

        /// <summary>
        /// Uses the name of the organism to search the database and find the kegg org code that corresponds to this organism.
        /// </summary>
        /// <param name="orgName">Name of the organism being customized</param>
        /// <param name="_databasePath">Path to the PBL database</param>
        /// <returns></returns>
        public static string GetKeggOrgCode(string orgName, string _databasePath)
        {
            string orgCode = "";
            using (var dbConnection = new SQLiteConnection("Datasource=" + _databasePath + ";Version=3;"))
            {
                dbConnection.Open();
                using (var cmd = new SQLiteCommand(dbConnection))
                {
                    var getOrgText = " SELECT kegg_org_code FROM organism WHERE taxon_name = \"" + orgName + "\" ;"; //add an OR kegg_org_name = "[orgname]" ? to make sure it covers all?
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
        /// This method will query the database to get a list of all the kegg genes which will be 
        /// used throughout the function to find and update other information.
        /// </summary>
        /// <param name="keggOrgCode">The KEGG organism code for the organism being worked on</param>
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
        /// <param name="keggOrgCode"> The KEGG organism code for the organism being worked on</param>
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
                            //Add the data for if it was observed or not
                            _keggGenes[Convert.ToString(reader["kegg_gene_id"])].IsObserved =
                                Convert.ToInt32(reader["is_observed"]);
                        }
                    }
                }
                dbConnection.Close();
            }
        }

        /// <summary>
        /// This method will search the PSM results file for this organism
        /// </summary>
        /// <param name="msgfFolder">Path to the PSM results file for this organism</param>
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
        /// Compare the proteins found in the msgf results to the ones in the database and mark which ones are observed
        /// </summary>
        /// <returns> The message of the results to display on the wizard review tab</returns>
        private static string DetermineObserved()
        {
            var listOfObserved = new List<string>();
            int newProteins = 0;
            var reviewResults = "";

            var observedCount = 0;
            var alreadyObserved = 0;
            foreach (var keggGene in _keggGenes.Values)
            {
                if (keggGene.IsObserved == 1)
                {
                    //keggGene was already observed so keep track.
                    listOfObserved.Add(keggGene.KeggGeneID);
                    alreadyObserved++;
                }
                foreach (var uniprot in _uniprots)
                {
                    if (uniprot == keggGene.UniprotAcc)
                    {
                        if (!listOfObserved.Contains(keggGene.KeggGeneID))
                        {
                            newProteins++;
                        }
                            keggGene.IsObserved = 1;
                            observedCount++;
                            break;                 
                    }
                }
            }

            //Compile the string of the results that will be returned and displayed on the user interface
            reviewResults = "We parsed the " + _msgfPaths.Count + " uploaded file(s) and found " + _peptides.Count + " peptides from " +
                             _uniprots.Count +
                            " proteins for organism " + _orgName + " (" + observedCount + " proteins mapped to KEGG pathways)." + " The plugin currently has " + alreadyObserved +
                            " proteins mapped to KEGG pathways for this organism. Your data has " + newProteins + " proteins that are not currently in the database." +
                            " Your selected option to supplement will combine the observed proteins from the plugin with the observed proteins found in your data.";
            //"The number of proteins that were already observed for " + _orgName + " is " + alreadyObserved + ". " +
            //                "The number of new proteins that were observed is " + observedCount + ". " +
            //                "The combined observed protein count is " + (alreadyObserved + observedCount) + ".";
            return reviewResults;
        }

        /// <summary>
        /// Commit and write changes to the database
        /// </summary>
        /// <param name="keggOrgCode"> The Kegg org code for the specified organism</param>
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
        /// Update the blib database to use the most current blibs and mark that it is now customized
        /// </summary>
        /// <param name="orgName"> Name of the organism</param>
        /// <param name="fileLoc"> Path to the file location of the blib</param>
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
                    var insertLast = string.Format(" VALUES ({0}{1}{0}, {0}{2}{0}); ", "\"", orgName, true);
                    cmd.CommandText = insertType + insertLast;
                    cmd.ExecuteNonQuery();
                }
                dbConnection.Close();
            }
        }
    }
}
