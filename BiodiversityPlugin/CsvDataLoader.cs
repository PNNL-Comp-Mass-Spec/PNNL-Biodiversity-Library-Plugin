using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BiodiversityPlugin.Models;

namespace BiodiversityPlugin
{
    public class CsvDataLoader : IDataAccess
    {
        public List<OrgPhylum> LoadOrganisms(string path)
        {
            var phylums = new Dictionary<string, OrgPhylum>();
            var classes = new Dictionary<string, OrgClass>();

            using (var reader = new StreamReader(path))
            {
                var header = reader.ReadLine();
                var row = reader.ReadLine();
                while (!string.IsNullOrWhiteSpace(row))
                {
                    var pieces = row.Split(',');
                    var org = new Organism(pieces[2], Convert.ToInt32(pieces[3]));
                    if (!classes.ContainsKey(pieces[1]))
                    {
                        classes[pieces[1]] = new OrgClass(pieces[1], new List<Organism>());
                    }
                    classes[pieces[1]].Organisms.Add(org);
                    
                    if (!phylums.ContainsKey(pieces[0]))
                    {
                        phylums[pieces[0]] = new OrgPhylum(pieces[0], new List<OrgClass>());
                    }
                    if (!phylums[pieces[0]].OrgClasses.Contains(classes[pieces[1]]))
                    {
                        phylums[pieces[0]].OrgClasses.Add(classes[pieces[1]]);
                    }

                    row = reader.ReadLine();
                }
            }

            var listPhy = phylums.Values.ToList();

            return listPhy;
        }

        public List<PathwayGroup> LoadPathways(string path)
        {
            var groups = new Dictionary<string, PathwayGroup>();

            using (var reader = new StreamReader(path))
            {
                var header = reader.ReadLine();
                var row = reader.ReadLine();
                while (!string.IsNullOrWhiteSpace(row))
                {
                    var pieces = row.Split(',');
                    var pathway = new Pathway(pieces[1], Convert.ToInt32(pieces[2]));
                    if (!groups.ContainsKey(pieces[0]))
                    {
                        groups[pieces[0]] = new PathwayGroup(pieces[0], new List<Pathway>());
                    }
                    groups[pieces[0]].Pathways.Add(pathway);
                    row = reader.ReadLine();
                }
            }

            var groupList = groups.Values.ToList();

            return groupList;

        }
    }
}
