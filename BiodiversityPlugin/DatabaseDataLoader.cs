using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Xml.Linq;
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

        public List<ProteinInformation> ExportAccessions(Pathway pathway, Organism org)
        {
            if (pathway == null || org == null)
            {
                return null;
            }

            var pathwayId = pathway.KeggId;
            var orgCode = org.OrgCode;
            var uniprotAccessions = new Dictionary<string, ProteinInformation>();
//            var uniprotAccessions = new List<ProteinInformation>();

            using (var dbConnection = new SQLiteConnection("Datasource=" + m_databasePath + ";Version=3;"))
            {
                dbConnection.Open();
                using (var cmd = new SQLiteCommand(dbConnection))
                {
                    var selectionText = 
                            string.Format(" SELECT refseq_uniprot_map.refseq_id " +
                                      " FROM kegg_gene_uniprot_map, observed_kegg_gene, refseq_uniprot_map, ncbi_protein " +
                                      " WHERE observed_kegg_gene.is_observed = 1 AND " +
                                      " observed_kegg_gene.kegg_pathway_id = '{0}' AND " +
                                      " observed_kegg_gene.kegg_org_code LIKE '%{1}%' AND " +
                                      " kegg_gene_uniprot_map.kegg_gene_id = observed_kegg_gene.kegg_gene_id AND " +
                                      " refseq_uniprot_map.uniprot_acc = kegg_gene_uniprot_map.uniprot_acc " +
                                      " group by refseq_uniprot_map.refseq_id",
                            pathwayId, orgCode);

                    
                    cmd.CommandText = selectionText;
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            uniprotAccessions.Add(reader.GetString(0), new ProteinInformation("Error: Not found in database", "Error: Not found in database", reader.GetString(0)));
                            //uniprotAccessions.Add(new ProteinInformation(reader.GetString(4), reader.GetString(5), reader.GetString(2)));
                        }
                    }
                    selectionText =
                        string.Format(" Select * " +
                                      " From ncbi_protein " +
                                      " Where refseq_id_versioned in(SELECT refseq_uniprot_map.refseq_id " +
                                      " FROM kegg_gene_uniprot_map, observed_kegg_gene, refseq_uniprot_map, ncbi_protein " +
                                      " WHERE observed_kegg_gene.is_observed = 1 AND " +
                                      " observed_kegg_gene.kegg_pathway_id = '{0}' AND " +
                                      " observed_kegg_gene.kegg_org_code LIKE '%{1}%' AND " +
                                      " kegg_gene_uniprot_map.kegg_gene_id = observed_kegg_gene.kegg_gene_id AND " +
                                      " refseq_uniprot_map.uniprot_acc = kegg_gene_uniprot_map.uniprot_acc " +
                                      " group by refseq_uniprot_map.refseq_id)",
                            pathwayId, orgCode);
                    cmd.CommandText = selectionText;
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            uniprotAccessions[reader.GetString(3)].Description = reader.GetString(5);
                            uniprotAccessions[reader.GetString(3)].Name = reader.GetString(4);
                            //uniprotAccessions.Add(new ProteinInformation(reader.GetString(4), reader.GetString(5), reader.GetString(2)));
                        }
                    }
                }
            }

            return uniprotAccessions.Values.ToList();
        }


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
    }
}
