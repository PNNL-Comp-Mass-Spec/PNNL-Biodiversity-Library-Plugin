using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.Linq;
using BiodiversityPlugin.Models;

namespace BiodiversityPlugin.DataManagement
{
    public class DatabaseDataLoader: IDataAccess
    {
        private readonly string m_databasePath;

        public DatabaseDataLoader(string organismDb)
        {
            m_databasePath = organismDb;
        }
        
        /// <summary>
        /// Method to populate organisms from the database
        /// </summary>
        /// <param name="organismList">List of organisms for use with filter box</param>
        /// <returns>List of Organisms, broken up into Phylum, class and the individual for the tree view</returns>
        public List<OrgPhylum> LoadOrganisms(ref List<string> organismList )
        {
            var phylums = new Dictionary<string, OrgPhylum>();
            var classes = new Dictionary<Tuple<string, string>, OrgClass>();

            using (var dbConnection = new SQLiteConnection("Datasource=" + m_databasePath + ";Version=3;"))
            {
                dbConnection.Open();
                using (var cmd = new SQLiteCommand(dbConnection))
                {
                    const string selectionText = "SELECT * FROM organism";
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
                                    if (ogclass == "")
                                    {
                                        ogclass = "Unclassified";
                                    }
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
                    organismList.AddRange(orgClass.Organisms.Select(organism => organism.Name));
                }
            }
            return phylums.Values.ToList();
        }

        /// <summary>
        /// Method to create the list of pathways from the database, excluding the
        /// Global and overview maps as there is no information in these pathways
        /// </summary>
        /// <returns>List of Pathways, broken up into Catagory, group and then individual for the tree view</returns>
        public List<PathwayCatagory> LoadPathways()
        {
            var catagories = new Dictionary<string, PathwayCatagory>();
            var groups = new Dictionary<string, PathwayGroup>();

            using (var dbConnection = new SQLiteConnection("Datasource=" + m_databasePath + ";Version=3;"))
            {
                dbConnection.Open();

                using (var cmd = new SQLiteCommand(dbConnection))
                {
                    const string selectionText = "SELECT * FROM kegg_pathway";
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
                            if (subCat != "Global and overview maps")
                            {
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
            }

            var catList = catagories.Values.ToList();

            return catList;
        }

        /// <summary>
        /// Method to determine the % coverage of a pathway by an organism in MSMS space.
        /// If there are no genes for the organism/pathway combination, then pathway's
        /// ContainsGenes flag is set to true and a percentage is set. Otherwise, what
        /// will display is "No Genes for this Pathway"
        /// </summary>
        /// <param name="org">Organism selected from Organisms tab</param>
        /// <param name="pathways">List of all possible pathways for an organism</param>
        public void LoadPathwayCoverage(Organism org, ref List<Pathway> pathways)
        {
            // If we don't have an org code for the organism, we cannot say with
            // confidence what the coverage is.
            if (org.OrgCode == "")
            {
                return;
            }
            // Reset all pathways to Not containing genes
            foreach (var pathway in pathways)
            {
                pathway.ContainsGenes = false;
            }

            using (var dbConnection = new SQLiteConnection("Datasource=" + m_databasePath + ";Version=3;"))
            {
                dbConnection.Open();

                using (var cmd = new SQLiteCommand(dbConnection))
                {
                    // Current database does not include organism/pathway combinations that contain 0 genes,
                    // if this changes, will need to add to the query.
                    string viewSelectionText = string.Format(" SELECT " +
                                                             " kegg_pathway_id, percent_observed_genes " +
                                                             " FROM vw_observed_kegg_organism_pathway " +
                                                             " WHERE kegg_org_code = \"{0}\" ", org.OrgCode);

                    cmd.CommandText = viewSelectionText;

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var pathId = reader.GetString(0);
                            foreach (var pathway in pathways.Where(pathway => pathway.KeggId == pathId))
                            {
                                pathway.ContainsGenes = true;
                                pathway.PercentCover = Convert.ToInt32(reader.GetDouble(1)*10000)/100.0;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Method to retrieve the Kegg Orthologs for the pathway which have been seen in MSMS space.
        /// Each KeggKoInformation in the returned list is assured of having a KeggId. KeggGeneName and
        /// KeggEc might be empty strings, but if there is information in the database for these fields, 
        /// it will be preserved and copied to the object.
        /// </summary>
        /// <param name="pathway">Pathway of interest</param>
        /// <param name="SelectedOrganism">Organism of interest</param>
        /// <returns>List of type KeggKoInformation, all of which were seen in MSMS space 
        /// for the pathway and organism of interest</returns>
        public List<KeggKoInformation> ExportKosWithData(Pathway pathway, Organism SelectedOrganism)
        {
            // If either brought in are null, return null because that'll
            // cause unintended workflow when the database query occurs
            if (pathway == null || SelectedOrganism == null)
            {
                return null;
            }

            var orgCode = SelectedOrganism.OrgCode;
            var koIds = new List<string>();
            var koInformation = new List<KeggKoInformation>();

            using (var dbConnection = new SQLiteConnection("Datasource=" + m_databasePath + ";Version=3;"))
            {
                dbConnection.Open();
                var stopwatch = new Stopwatch();
                stopwatch.Start();
                using (var cmd = new SQLiteCommand(dbConnection))
                {
                    // Query is set up in this way to facilitate faster query, returning the sum total of Kegg Ortholog infomations
                    // Paring down the information returned takes place below. 
                    var selectionText =
                            string.Format(" SELECT observed_kegg_gene.is_observed, observed_kegg_gene.kegg_pathway_id, observed_kegg_gene.kegg_org_code, kegg_gene_ko_map.kegg_ko_id, " +
                                      " kegg_ko.kegg_gene_name, kegg_ko.kegg_ec " +
                                      " FROM kegg_gene_ko_map, observed_kegg_gene, kegg_ko " +
                                      " WHERE kegg_gene_ko_map.kegg_gene_id = observed_kegg_gene.kegg_gene_id " + 
                                      " AND kegg_ko.kegg_ko_id = kegg_gene_ko_map.kegg_ko_id");


                    cmd.CommandText = selectionText;
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            // Only care about if the gene was observed, is in the pathway of interest,
                            // contains the org code for the organism in question and has not already been seen
                            if (reader.GetInt32(0) == 1 && pathway.KeggId.Contains(reader.GetString(1)) &&
                                reader.GetString(2).Contains(orgCode) && !koIds.Contains(reader.GetString(3)))
                            {
                                koIds.Add(reader.GetString(3));
                                var koToAdd = new KeggKoInformation(reader.GetString(3), "", "")
                                {
                                    KeggGeneName = !reader.IsDBNull(4) ? reader.GetString(4) : "",
                                    KeggEc = !reader.IsDBNull(5) ? reader.GetString(5) : ""
                                };
                                koInformation.Add(koToAdd);
                            }
                        }
                    }
                }
            }

            return koInformation;
        }

        /// <summary>
        /// Method to retrieve the Kegg Orthologs for the pathway which have not been seen in MSMS space.
        /// Each KeggKoInformation in the returned list is assured of having a KeggId. KeggGeneName and
        /// KeggEc might be empty strings, but if there is information in the database for these fields, 
        /// it will be preserved and copied to the object.
        /// </summary>
        /// <param name="pathway">Pathway of interest</param>
        /// <param name="SelectedOrganism">Organism of interest</param>
        /// <returns>List of type KeggKoInformation, all of which were not seen in MSMS space 
        /// for the pathway and organism of interest</returns>
        public List<KeggKoInformation> ExportKosWithoutData(Pathway pathway, Organism SelectedOrganism)
        {
            // If either brought in are null, return null because that'll
            // cause unintended workflow when the database query occurs
            if (pathway == null || SelectedOrganism == null)
            {
                return null;
            }
            var orgCode = SelectedOrganism.OrgCode;
            var koIds = new List<string>();
            var koInformation = new List<KeggKoInformation>();

            using (var dbConnection = new SQLiteConnection("Datasource=" + m_databasePath + ";Version=3;"))
            {
                dbConnection.Open();
                var stopwatch = new Stopwatch();
                stopwatch.Start();
                using (var cmd = new SQLiteCommand(dbConnection))
                {
                    // Query is set up in this way to facilitate faster query, returning the sum total of Kegg Ortholog infomations
                    // Paring down the information returned takes place below. 
                    var selectionText =
                            string.Format(" SELECT observed_kegg_gene.is_observed, observed_kegg_gene.kegg_pathway_id, observed_kegg_gene.kegg_org_code, kegg_gene_ko_map.kegg_ko_id, " +
                                      " kegg_ko.kegg_gene_name, kegg_ko.kegg_ec " +
                                      " FROM kegg_gene_ko_map, observed_kegg_gene, kegg_ko " +
                                      " WHERE kegg_gene_ko_map.kegg_gene_id = observed_kegg_gene.kegg_gene_id " +
                                      " AND kegg_ko.kegg_ko_id = kegg_gene_ko_map.kegg_ko_id");


                    cmd.CommandText = selectionText;
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            // Only care about if the gene was not observed, is in the pathway of interest,
                            // contains the org code for the organism in question and has not already been seen
                            if (reader.GetInt32(0) == 0 && pathway.KeggId.Contains(reader.GetString(1)) &&
                                reader.GetString(2).Contains(orgCode) && !koIds.Contains(reader.GetString(3)))
                            {
                                koIds.Add(reader.GetString(3));
                                var koToAdd = new KeggKoInformation(reader.GetString(3), "", "")
                                {
                                    KeggGeneName = !reader.IsDBNull(4) ? reader.GetString(4) : "",
                                    KeggEc = !reader.IsDBNull(5) ? reader.GetString(5) : ""
                                };
                                koInformation.Add(koToAdd);
                            }
                        }
                    }
                }
            }

            return koInformation;
        }

        /// <summary>
        /// Method to retrieve the ProteinInformations for the Selected Kegg Orthologs for the pathways
        /// and organism in question. Each ProteinInformation in the returned list is assured of having 
        /// a refseq Id, and if it exists for the refseq ID, the description and versioned refseq as well
        /// </summary>
        /// <param name="pathways">List of Pathways of interest</param>
        /// <param name="org">Organism of interest</param>
        /// <returns>List of type ProteinInformation for the pathway and organism of interest</returns>
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
