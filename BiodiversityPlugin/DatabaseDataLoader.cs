using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder;
using System.Data.SQLite;
using System.Data.SQLite.Linq.Properties;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using BiodiversityPlugin.Models;

namespace BiodiversityPlugin
{
    public class DatabaseDataLoader: IDataAccess
    {
        private string m_databasePath = "Resources/PBL.db";

        public List<string> ExportAccessions(Pathway pathway, Organism org)
        {
            if (pathway == null || org == null)
            {
                return null;
            }

            var pathwayId = pathway.KeggId;
            var orgCode = org.OrgCode;
            var uniprotAccessions = new List<string>();

            using (var dbConnection = new SQLiteConnection("Datasource=" + m_databasePath + ";Version=3;"))
            {
                dbConnection.Open();
                using (var cmd = new SQLiteCommand(dbConnection))
                {
                    var selectionText =
                        string.Format("SELECT kegg_gene_uniprot_map.uniprot_acc FROM kegg_gene_uniprot_map, " +
                                      " observed_kegg_gene WHERE observed_kegg_gene.is_observed = 1 AND " +
                                      " observed_kegg_gene.kegg_pathway_id = '{0}' AND " +
                                      " observed_kegg_gene.kegg_org_code LIKE '%{1}%' AND " +
                                      "kegg_gene_uniprot_map.kegg_gene_id = observed_kegg_gene.kegg_gene_id",
                            pathwayId, orgCode);
                    cmd.CommandText = selectionText;
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            uniprotAccessions.Add(reader.GetString(0));
                        }
                    }
                }
            }

            return uniprotAccessions;
        }


        public List<OrgPhylum> LoadOrganisms(string path)
        {
            

            throw new NotImplementedException();
        }

        public List<PathwayCatagory> LoadPathways(string path)
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
