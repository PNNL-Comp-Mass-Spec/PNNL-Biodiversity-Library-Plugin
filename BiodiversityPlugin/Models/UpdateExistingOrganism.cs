using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using KeggParsesClassLibrary;

//using System.Threading.Tasks;
//using KeggParsesClassLibrary;

namespace BiodiversityPlugin.Models
{
    public class UpdateExistingOrganism
    {
        private static Dictionary<string, KeggGene> _keggGenes = new Dictionary<string, KeggGene>();
        private static Dictionary<string, List<Tuple<string, int>>> _proteinPeptideMap = new Dictionary<string, List<Tuple<string, int>>>();
        private static List<string> _refseqs = new List<string>();
        private static List<Tuple<string, int>> _peptides = new List<Tuple<string, int>>();

        public static void UpdateExisting(string orgName, string blibLoc, string msgfFolderLoc, string _databasePath)
        {
            //Call all the methods here that will update the existing organism
            //TODO: we can get rid of the org code if i make a method to pull org code based on organism name
            string orgcode = ""; //call  method to set org code like in biodiv org adder
            GetKeggGenes(orgcode);
            GetConnectedPathways(orgcode, _keggGenes);
            SearchMsgfFiles(msgfFolderLoc);
            DetermineObserved();
            UpdateObservedKeggGeneTable(orgcode, _keggGenes, _databasePath);
            UpdateBlibLocation(orgName, blibLoc, _databasePath);
        }

        private static void GetKeggGenes(string keggOrgCode)
        {
            var options = StringSplitOptions.RemoveEmptyEntries;
            char[] lineSplit = { '\n' };
            var keggGeneUrl = WebRequest.Create("http://rest.kegg.jp/list/" + keggOrgCode);
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
                            //Add gene id to the dictionary
                            _keggGenes.Add(geneID, new KeggGene(keggOrgCode, geneID));
                        }
                    }
                }
            }
        }

        private static void GetConnectedPathways(string keggOrgCode, Dictionary<string, KeggGene> _keggGenes)
        {
            var options = StringSplitOptions.RemoveEmptyEntries;
            char[] lineSplit = { '\n' };
            var lines = new List<string>();
            var PathwayGeneListUrl = WebRequest.Create("http://rest.kegg.jp/link/pathway/" + keggOrgCode);
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

        private static void DetermineObserved()
        {
            foreach (var keggGene in _keggGenes.Values)
            {
                foreach (var refseq in _refseqs)
                {
                    if (refseq.Split('.').First() == keggGene.RefseqID)
                    {
                        keggGene.IsObserved = 1;
                        break;
                    }
                }
            }
        }

        private static void UpdateObservedKeggGeneTable(string keggOrgCode, Dictionary<string, KeggGene> _keggGenes, string _databasePath)
        {
            using (var dbConnection = new SQLiteConnection("Datasource=" + _databasePath + ";Version=3;"))
            {
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
                }
            }
        }

        private static void UpdateBlibLocation(string orgName, string fileLoc, string _databasePath)
        {
            var fileLocSource = _databasePath.Replace("PBL.db", "blibFileLoc.db");

            using (var dbConnection = new SQLiteConnection("Datasource=" + fileLocSource + ";Version=3;"))
            {
                dbConnection.Open();
                using (var cmd = new SQLiteCommand(dbConnection))
                {
                    var insertUpdate = " INSERT OR REPLACE INTO fileLocation ( \"" + orgName + "\", \"" + fileLoc + "\" ) WHERE orgName = \"" + orgName + "\"; ";
                    cmd.CommandText = insertUpdate;
                    cmd.ExecuteNonQuery();
                }
                dbConnection.Close();
            }
        }
    }
}
