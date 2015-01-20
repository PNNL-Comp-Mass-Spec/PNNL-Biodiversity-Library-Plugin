﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BiodiversityPlugin.Models;

namespace BiodiversityPlugin.DataManagement
{
    [Obsolete]
    public class CsvDataLoader : IDataAccess
    {
        private readonly string _organisms;
        private readonly string _pathways;

        /// <summary>
        /// Constructor for the CSV dataloader for the application.
        /// This is used (temporarily) to get a list of organisms and pathways
        /// that will be seen in the database.
        /// </summary>
        /// <param name="organisms"></param>
        /// <param name="pathways"></param>
        public CsvDataLoader(string organisms, string pathways)
        {
            _organisms = organisms;
            _pathways = pathways;
        }

        public List<OrgPhylum> LoadOrganisms(ref List<string> organismList)
        {
            
            var phylums = new Dictionary<string, OrgPhylum>();
            var classes = new Dictionary<Tuple<string, string>, OrgClass>();

            using (var reader = new StreamReader(_organisms))
            {
                // Read, and then discard, the header line of the file.
                reader.ReadLine();

                var row = reader.ReadLine();
                while (!string.IsNullOrWhiteSpace(row))
                {
                    var pieces = row.Split('\t');
                    var org = new Organism(pieces[2], Convert.ToInt32(pieces[3]), pieces[4]);
                    var pair = new Tuple<string, string>(pieces[0], pieces[1]);
                    if (!classes.ContainsKey(pair))
                    {
                        classes[pair] = new OrgClass(pieces[1], new List<Organism>());
                    }
                    classes[pair].Organisms.Add(org);
                    organismList.Add(org.Name);
                    
                    if (!phylums.ContainsKey(pieces[0]))
                    {
                        phylums[pieces[0]] = new OrgPhylum(pieces[0], new List<OrgClass>());
                    }
                    if (!phylums[pieces[0]].OrgClasses.Contains(classes[pair]))
                    {
                        phylums[pieces[0]].OrgClasses.Add(classes[pair]);
                    }

                    row = reader.ReadLine();
                }
            }

            var listPhy = phylums.Values.ToList();

            return listPhy;
        }

        public List<PathwayCatagory> LoadPathways()
        {
            var groups = new Dictionary<string, PathwayGroup>();

            using (var reader = new StreamReader(_pathways))
            {
                // Read, and then discard, the header line of the file.
                reader.ReadLine();

                var row = reader.ReadLine();
                while (!string.IsNullOrWhiteSpace(row))
                {
                    var pieces = row.Split('\t');
                    var pathway = new Pathway(pieces[1], pieces[2]);
                    if (!groups.ContainsKey(pieces[0]))
                    {
                        groups[pieces[0]] = new PathwayGroup(pieces[0], new List<Pathway>());
                    }
                    groups[pieces[0]].Pathways.Add(pathway);
                    row = reader.ReadLine();
                }
            }

            var groupList = groups.Values.ToList();
            var cats = new List<PathwayCatagory> {new PathwayCatagory("Metabolism", groupList)};
            return cats;

        }
    }
}