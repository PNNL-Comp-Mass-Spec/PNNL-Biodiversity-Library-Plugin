using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using BiodiversityPlugin.ViewModels;
using KeggParsesClassLibrary;

namespace BiodiversityPlugin.Models
{
    class SupplementOrgansim
    {
        private static Dictionary<string, KeggGene> _keggGenes = new Dictionary<string, KeggGene>();
        private static Dictionary<string, List<Tuple<string, int>>> _proteinPeptideMap = new Dictionary<string, List<Tuple<string, int>>>();
        private static List<string> _refseqs = new List<string>();
        private static List<Tuple<string, int>> _peptides = new List<Tuple<string, int>>();
        private static string _databasePath;

        public void Supplement(string orgName, string blibLoc, string msgfFolderLoc, string databasePath)
        {
            _databasePath = databasePath;
            string orgcode = GetKeggOrgCode(orgName);
            GetKeggGenesWithRefs(orgcode);
            GetConnectedPathways(orgcode);
            SearchMsgfFiles(msgfFolderLoc);
            DetermineObserved(orgcode, blibLoc, orgName);
        }

        private static string GetKeggOrgCode(string orgName)
        {
            string orgCode = "";
            using (var dbConnection = new SQLiteConnection("Datasource=" + _databasePath + ";Version=3;"))
            {
                dbConnection.Open();
                using (var cmd = new SQLiteCommand(dbConnection))
                {
                    var getOrgText = " SELECT kegg_org_code FROM organism WHERE ncbi_taxon_name = \"" + orgName + "\" ;"; //add an OR kegg_org_name = "[orgname]" ? to make sure it covers all?
                    cmd.CommandText = getOrgText;
                    SQLiteDataReader reader = cmd.ExecuteReader();
                    if (reader.Read())
                    {
                        orgCode = Convert.ToString(reader[0]);
                    }
                }
            }
            return orgCode;
        }

        private static void GetKeggGenesWithRefs(string keggOrgCode)
        {
            using (var dbConnection = new SQLiteConnection("Datasource=" + _databasePath + ";Version=3;"))
            {
                dbConnection.Open();
                using (var cmd = new SQLiteCommand(dbConnection))
                {
                    var getOrgText = " SELECT kegg_gene_id, refseq_id FROM kegg_gene WHERE kegg_org_code = \"" + keggOrgCode + "\" ;";
                    cmd.CommandText = getOrgText;
                    SQLiteDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        //Add the kegg gene to dictionary
                        _keggGenes.Add(Convert.ToString(reader["kegg_gene_id"]),
                            new KeggGene(keggOrgCode, Convert.ToString(reader["kegg_gene_id"])));

                        //Pull the refseqs for that organism
                        _keggGenes[Convert.ToString(reader["kegg_gene_id"])].RefseqID =
                            Convert.ToString(reader["refseq_id"]);
                    }
                }
                dbConnection.Close();
            }
        }

        private static void GetConnectedPathways(string keggOrgCode)
        {
            using (var dbConnection = new SQLiteConnection("Datasource=" + _databasePath + ";Version=3;"))
            {
                dbConnection.Open();
                using (var cmd = new SQLiteCommand(dbConnection))
                {
                    var getOrgText = " SELECT * FROM observed_kegg_gene WHERE kegg_org_code = \"" + keggOrgCode + "\" ;";
                    cmd.CommandText = getOrgText;
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

        private static void SearchMsgfFiles(string msgfFolder)
        {
            var dirs = Directory.GetDirectories(msgfFolder).ToList();
            dirs.Add(msgfFolder);
            double cutoff = 0.0001;

            foreach (var file in Directory.GetFiles(msgfFolder))
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
                            if (Convert.ToDouble(pieces[qValIndex]) < cutoff && pieces[protInd].Split('|').Count() > 1)
                            {
                                var peptide = pieces[pepInd].Split('.')[1];
                                var prot = pieces[protInd].Split('|')[1].Split('.')[0];
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

        private static void DetermineObserved(string orgcode, string blibLoc, string orgName)
        {
            var observedCount = 0;
            foreach (var keggGene in _keggGenes.Values)
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
            var result = MessageBox.Show("The new combined observed protein count is " + observedCount + ". Would you like to continue? ", "Search Complete",
                MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                UpdateObservedKeggGeneTable(orgcode);
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

        private static void UpdateObservedKeggGeneTable(string keggOrgCode)
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

        public static void UpdateBlibLocation(string orgName, string fileLoc)
        {
            var fileLocSource = _databasePath.Replace("PBL.db", "blibFileLoc.db");

            using (var dbConnection = new SQLiteConnection("Datasource=" + fileLocSource + ";Version=3;"))
            {
                dbConnection.Open();
                using (var cmd = new SQLiteCommand(dbConnection))
                {
                    var insertUpdate = " INSERT INTO fileLocation (orgName, fileLocation, custom)";
                    var lastBit = string.Format(" VALUES ({0}{1}{0}, {0}{2}{0}, {0}{3}{0}); ", "\"", orgName, fileLoc, true);
                    cmd.CommandText = insertUpdate + lastBit;
                    cmd.ExecuteNonQuery();

                    var insertType = "INSERT INTO customOrganisms (orgName, bothBlibs)";
                    var insertLast = string.Format(" VALUES ({0}{1}{0}, {0}{2}{0}); ", "\"", orgName, true);
                    cmd.CommandText = insertType + insertLast;
                    cmd.ExecuteNonQuery();
                }
                dbConnection.Close();
                MessageBox.Show("Organism and blib file location have been updated successfully.", "Finished!");
            }
        }
    }
}
