using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using KeggParsesClassLibrary;

namespace BiodiversityPlugin.Models
{
    class InsertNewOrganism
    {
        private static Dictionary<string, KeggGene> _keggGenes = new Dictionary<string, KeggGene>();
        private static string _keggOrgCode;
        private static string _orgDomain;
        private static string _orgKingdom;
        private static string _orgPhylum;
        private static string _orgClass;
        private static string _orgName;

        public static void InsertNew(string orgName, string blibLoc, List<string> msgfFolderLoc, string databasePath, string orgCode) //pass in an optional _keggOrgCode?
        {
            _keggOrgCode = orgCode;
            FindOrgCode(orgName);
            DownloadKeggGenes(_keggOrgCode);
        }

        private static void FindOrgCode(string orgName)
        {
            var options = StringSplitOptions.RemoveEmptyEntries;
            var numPieces = orgName.Split(' ').Count();
            Console.WriteLine(orgName);

            var eListUrl = WebRequest.Create("http://rest.kegg.jp/list/organism");
            var listStream = eListUrl.GetResponse().GetResponseStream();
            var lines = new List<string>();
            using (var listReader = new StreamReader(listStream))
            {
                while (listReader.Peek() > -1)
                {
                    var wholeFile = listReader.ReadToEnd();
                    char[] lineSplit = { '\n' };
                    lines = wholeFile.Split(lineSplit, options).ToList();
                }
            }

            if (!string.IsNullOrWhiteSpace(_keggOrgCode))
            {
                // we know the organism, but need to know the taxon info for it
                foreach (var line in lines)
                {
                    if (line.Split('\t')[1] == _keggOrgCode)
                    {
                        _keggOrgCode = line.Split('\t')[1];
                        var orgTaxon = line.Split('\t')[3];
                        _orgDomain = orgTaxon.Split(';')[0];
                        _orgKingdom = orgTaxon.Split(';')[1];
                        _orgPhylum = orgTaxon.Split(';')[2];
                        _orgClass = orgTaxon.Split(';')[3];
                        _orgName = line.Split('\t')[2];
                    }
                }
            }
            else
            {

                var bestMatch = 0;
                foreach (var line in lines)
                {
                    var curMatch = 0;
                    var piece = 0;
                    while (piece < numPieces && piece < line.Split('\t')[2].Split(' ').Count())
                    {
                        if (line.Split('\t')[2].Split(' ')[piece] == orgName.Split(' ')[piece])
                        {
                            curMatch++;
                        }
                        piece++;
                    }
                    if (curMatch > bestMatch)
                    {
                        Console.WriteLine("New best match organism: " + line.Split('\t')[2]);
                        _keggOrgCode = line.Split('\t')[1];
                        var orgTaxon = line.Split('\t')[3];
                        _orgDomain = orgTaxon.Split(';')[0];
                        _orgKingdom = orgTaxon.Split(';')[1];
                        _orgPhylum = orgTaxon.Split(';')[2];
                        _orgClass = orgTaxon.Split(';')[3];
                        _orgName = line.Split('\t')[2];
                        bestMatch = curMatch;
                    }
                }
            }
        }

        private static void DownloadKeggGenes(string _keggOrgCode)
        {
            var options = StringSplitOptions.RemoveEmptyEntries;
            char[] lineSplit = { '\n' };

            var keggGeneUrl = WebRequest.Create("http://rest.kegg.jp/list/" + _keggOrgCode);
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
                        _keggGenes.Add(geneID, new KeggGene(_keggOrgCode, geneID));
                    }
                }
            }
        }
    }
}
