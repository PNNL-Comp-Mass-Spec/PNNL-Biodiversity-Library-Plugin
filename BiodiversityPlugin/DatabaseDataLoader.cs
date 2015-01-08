using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.Linq;
using BiodiversityPlugin.Models;

namespace BiodiversityPlugin
{
    public class DatabaseDataLoader: IDataAccess
    {
        //private string m_databasePath = "Tools/BioDiversity/DataFiles/PBL.db";
        private readonly string m_databasePath;

        public DatabaseDataLoader(string organismDb)
        {
            m_databasePath = organismDb;
        }

        public void LoadAccessions()
        {
            
        }

          // OLD STYLE OF EXPORTING ACCESSIONS
//        public List<ProteinInformation> ExportAccessions(List<Pathway> pathways, Organism org)
//        {
//            if (pathways == null || org == null)
//            {
//                return null;
//            }
//            var pathwayIds = new List<string>();
//            foreach (var pathway in pathways)
//            {
//                pathwayIds.Add(pathway.KeggId);
//            }
//            var orgCode = org.OrgCode;
//            var uniprotAccessions = new Dictionary<string, ProteinInformation>();
////            var uniprotAccessions = new List<ProteinInformation>();

//            using (var dbConnection = new SQLiteConnection("Datasource=" + m_databasePath + ";Version=3;"))
//            {
//                dbConnection.Open();
//                var stopwatch = new Stopwatch();
//                stopwatch.Start();
//                using (var cmd = new SQLiteCommand(dbConnection))
//                {
//                    var selectionText =
//                            string.Format(" SELECT refseq_uniprot_map.refseq_id, observed_kegg_gene.is_observed, observed_kegg_gene.kegg_pathway_id, observed_kegg_gene.kegg_org_code " +
//                                      " FROM kegg_gene_uniprot_map, observed_kegg_gene, refseq_uniprot_map " +
//                                      " WHERE kegg_gene_uniprot_map.kegg_gene_id = observed_kegg_gene.kegg_gene_id AND " +
//                                      " refseq_uniprot_map.uniprot_acc = kegg_gene_uniprot_map.uniprot_acc ");//,
////                            pathwayId, orgCode);

                    
//                    cmd.CommandText = selectionText;
//                    using (var reader = cmd.ExecuteReader())
//                    {
//                        while (reader.Read())
//                        {
//                            var obs = reader.GetInt32(1);
//                            var pat = reader.GetString(2);
//                            var or  = reader.GetString(3);
//                            //if (pat == "00010")
//                            //{
//                                //Console.WriteLine(string.Format("Should match {0}", (reader.GetString(2) == pathwayId)));
//                                //if (or == "acr")
//                                    //Console.WriteLine(string.Format("Should return true: {0}",
//                                    //    reader.GetString(3).Contains(orgCode)));
//                            //}
//                            if(reader.GetInt32(1) == 1 && pathwayIds.Contains(reader.GetString(2)) && reader.GetString(3).Contains(orgCode) && !uniprotAccessions.ContainsKey(reader.GetString(0)))
//                                uniprotAccessions.Add(reader.GetString(0), new ProteinInformation("Not found in database", "Not found in database", reader.GetString(0)));
//                            //uniprotAccessions.Add(new ProteinInformation(reader.GetString(4), reader.GetString(5), reader.GetString(2)));
//                        }
//                    }
//                    stopwatch.Stop();

//                    var ts = stopwatch.ElapsedTicks;
//                    Console.WriteLine(string.Format("Pulling refSeq took {0} ticks",ts));
//                    stopwatch.Reset();
//                    stopwatch.Start();
//                    selectionText =
//                        string.Format(" Select * " +
//                                      " From ncbi_protein " +
//                                      " Where refseq_id_versioned in( '" + String.Join("', '",uniprotAccessions.Keys) + "')");
//                    cmd.CommandText = selectionText;
//                    using (var reader = cmd.ExecuteReader())
//                    {
//                        while (reader.Read())
//                        {
//                            uniprotAccessions[reader.GetString(3)].Description = reader.IsDBNull(5) ? "": reader.GetString(5);
//                            uniprotAccessions[reader.GetString(3)].Name = reader.GetString(4);
//                            //uniprotAccessions.Add(new ProteinInformation(reader.GetString(4), reader.GetString(5), reader.GetString(2)));
//                        }
//                    }
//                    stopwatch.Stop();
//                    ts = stopwatch.ElapsedTicks;
//                    Console.WriteLine(string.Format("Pulling protData took {0} ticks", ts));
//                }
//            }

//            return uniprotAccessions.Values.ToList();
//        }


        public List<OrgPhylum> LoadOrganisms()
        {
            var phylums = new Dictionary<string, OrgPhylum>();
            var classes = new Dictionary<Tuple<string, string>, OrgClass>();

            using (var dbConnection = new SQLiteConnection("Datasource=" + m_databasePath + ";Version=3;"))
            {
                dbConnection.Open();
                using (var cmd = new SQLiteCommand(dbConnection))
                {
                    var selectionText = "SELECT * FROM organism";
                    cmd.CommandText = selectionText;
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var taxon = Convert.ToInt32(reader["ncbi_taxon_id"]);
                            var taxonName = reader["ncbi_taxon_name"].ToString();
                            var orgCode = reader["kegg_org_code"].ToString();
                            using (var cmd1 = new SQLiteCommand(dbConnection))
                            {
                                cmd1.CommandText = String.Format("SELECT * FROM organism_taxonomy WHERE ncbi_taxon_id = {0};",
                                    taxon);
                                var reader1 = cmd1.ExecuteReader();
                                while (reader1.Read())
                                {
                                    var ogphylum = reader1["og_phylum"].ToString();
                                    var ogclass = reader1["og_class"].ToString();
                                    var pair = new Tuple<string, string>(ogphylum, ogclass);
                                    var org = new Organism(taxonName, taxon, orgCode);

                                    if (!classes.ContainsKey(pair))
                                    {
                                        classes.Add(pair, new OrgClass(ogclass, new List<Organism>()));
                                    }
                                    classes[pair].Organisms.Add(org);

                                    if (!phylums.ContainsKey(ogphylum))
                                    {
                                        phylums.Add(ogphylum, new OrgPhylum(ogphylum, new List<OrgClass>()));
                                    }
                                    if (!phylums[ogphylum].OrgClasses.Contains(classes[pair]))
                                    {
                                        phylums[ogphylum].OrgClasses.Add(classes[pair]);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            foreach (var phylum in phylums.Values)
            {
                phylum.OrgClasses.Sort((x, y) => x.ClassName.CompareTo(y.ClassName));
                foreach (var orgClass in phylum.OrgClasses)
                {
                    orgClass.Organisms.Sort((x, y) => x.Name.CompareTo(y.Name));
                }
            }
            return phylums.Values.ToList();
        }

        public List<PathwayCatagory> LoadPathways()
        {
            var catagories = new Dictionary<string, PathwayCatagory>();
            var groups = new Dictionary<string, PathwayGroup>();

            using (var dbConnection = new SQLiteConnection("Datasource=" + m_databasePath + ";Version=3;"))
            {
                dbConnection.Open();

                using (var cmd = new SQLiteCommand(dbConnection))
                {
                    var selectionText = "SELECT * FROM kegg_pathway";
                    cmd.CommandText = selectionText;
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var subCat = reader.GetString(2);
                            var pathName = reader.GetString(3);
                            var pathId = reader.GetString(0);
                            var catName = reader.GetString(1);

                            var pathway = new Pathway(pathName, pathId);

                            if (!groups.ContainsKey(subCat))
                            {
                                groups[subCat] = new PathwayGroup(subCat, new List<Pathway>());
                            }
                            groups[subCat].Pathways.Add(pathway);

                            if (!catagories.ContainsKey(catName))
                            {
                                catagories[catName] = new PathwayCatagory(catName, new List<PathwayGroup>());
                            }
                            if (!catagories[catName].PathwayGroups.Contains(groups[subCat]))
                            {
                                catagories[catName].PathwayGroups.Add(groups[subCat]);
                            }
                            
                        }
                    }
                }
            }

            var catList = catagories.Values.ToList();

            return catList;
        }

        internal List<KeggKoInformation> ExportKosWithData(Pathway pathway, Organism SelectedOrganism)
        {
            if (pathway == null || SelectedOrganism == null)
            {
                return null;
            }
            var orgCode = SelectedOrganism.OrgCode;
            var KoIds = new List<string>();
            var koInformation = new List<KeggKoInformation>();
            //            var uniprotAccessions = new List<ProteinInformation>();

            using (var dbConnection = new SQLiteConnection("Datasource=" + m_databasePath + ";Version=3;"))
            {
                dbConnection.Open();
                var stopwatch = new Stopwatch();
                stopwatch.Start();
                using (var cmd = new SQLiteCommand(dbConnection))
                {
                    var selectionText =
                            string.Format(" SELECT observed_kegg_gene.is_observed, observed_kegg_gene.kegg_pathway_id, observed_kegg_gene.kegg_org_code, kegg_gene_ko_map.kegg_ko_id, " +
                                      " kegg_ko.kegg_gene_name, kegg_ko.kegg_ec " +
                                      " FROM kegg_gene_ko_map, observed_kegg_gene, kegg_ko " +
                                      " WHERE kegg_gene_ko_map.kegg_gene_id = observed_kegg_gene.kegg_gene_id " + 
                                      " AND kegg_ko.kegg_ko_id = kegg_gene_ko_map.kegg_ko_id");//,
                    //                            pathwayId, orgCode);


                    cmd.CommandText = selectionText;
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (reader.GetInt32(0) == 1 && pathway.KeggId.Contains(reader.GetString(1)) &&
                                reader.GetString(2).Contains(orgCode) && !KoIds.Contains(reader.GetString(3)))
                            {
                                KoIds.Add(reader.GetString(3));
                                var koToAdd = new KeggKoInformation(reader.GetString(3), "", "");
                                koToAdd.KeggGeneName = !reader.IsDBNull(4) ? reader.GetString(4) : "";
                                koToAdd.KeggEc = !reader.IsDBNull(5) ? reader.GetString(5) : "";
                                koInformation.Add(koToAdd);
                            }
                        }
                    }
                }
            }

            return koInformation;
        }


        public List<ProteinInformation> ExportAccessions(List<Pathway> pathways, Organism org)
        {
            if (pathways == null || org == null)
            {
                return null;
            }

            var orgCode = org.OrgCode;
            var uniprotAccessions = new Dictionary<string, ProteinInformation>();



            using (var dbConnection = new SQLiteConnection("Datasource=" + m_databasePath + ";Version=3;"))
            {
                dbConnection.Open();
                var stopwatch = new Stopwatch();
                stopwatch.Start();
                using (var cmd = new SQLiteCommand(dbConnection))
                {
                    foreach (var pathway in pathways)
                    {
                        if (pathway.SelectedKo.Count > 0)
                        {
                            var Kos = pathway.SelectedKo.Aggregate((working, next) => working + "\", " + '"' + next);
                            var
                                selectionText =
                                    string.Format(
                                        " SELECT refseq_uniprot_map.refseq_id, observed_kegg_gene.is_observed, observed_kegg_gene.kegg_pathway_id, observed_kegg_gene.kegg_org_code" +
                                        " FROM kegg_gene_ko_map, kegg_gene_uniprot_map, observed_kegg_gene, refseq_uniprot_map " +
                                        " WHERE observed_kegg_gene.kegg_gene_id in (SELECT kegg_gene_ko_map.kegg_gene_id " +
                                        " FROM kegg_gene_ko_map " +
                                        " WHERE kegg_gene_ko_map.kegg_ko_id in (\"{0}\")) AND " +
                                        " kegg_gene_uniprot_map.kegg_gene_id = observed_kegg_gene.kegg_gene_id AND " +
                                        " kegg_gene_ko_map.kegg_gene_id = observed_kegg_gene.kegg_gene_id AND " +
                                        " refseq_uniprot_map.uniprot_acc = kegg_gene_uniprot_map.uniprot_acc  ", Kos);


                            cmd.CommandText = selectionText;
                            using (var reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {

                                    var obs = reader.GetInt32(1);
                                    var pat = reader.GetString(2);
                                    var or = reader.GetString(3);
                                    if (reader.GetInt32(1) == 1 && pathway.KeggId == reader.GetString(2) &&
                                        reader.GetString(3).Contains(orgCode) &&
                                        !uniprotAccessions.ContainsKey(reader.GetString(0)))
                                        uniprotAccessions.Add(reader.GetString(0),
                                            new ProteinInformation("Not found in database", "Not found in database",
                                                reader.GetString(0)));
                                }
                            }
                            selectionText =
                                string.Format(" Select * " +
                                              " From ncbi_protein " +
                                              " Where refseq_id_versioned in( '" +
                                              String.Join("', '", uniprotAccessions.Keys) + "')");
                            cmd.CommandText = selectionText;
                            using (var reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    uniprotAccessions[reader.GetString(3)].Description = reader.IsDBNull(5)
                                        ? ""
                                        : reader.GetString(5);
                                    uniprotAccessions[reader.GetString(3)].Name = reader.GetString(4);
                                }
                            }
                        }
                    }
                }
            }

            return uniprotAccessions.Values.ToList();
        }
    }
}
