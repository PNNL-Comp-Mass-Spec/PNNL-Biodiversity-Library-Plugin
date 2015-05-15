using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;

namespace BiodiversityPlugin.IO
{
    public class TextExporter
    {
        /// <summary>
        /// This class is used to store a method to export files.
        /// </summary>
        private static TextExporter exporterInstance;

        private string _dbPath;

        private TextExporter(string dbPath)
        {
            _dbPath = dbPath;
        }

        public static void createInstance(string dbPath)
        {
            if (exporterInstance == null)
            {
                exporterInstance = new TextExporter(dbPath);
            }
            else
            {
                Console.WriteLine("There was an instance already of the exporter");
            }
        }

        public static TextExporter getExporter()
        {
            if (exporterInstance != null)
            {
                return exporterInstance;
            }
                
            throw new Exception("Undefined database path TextExporter Class");
        }

        public void Export(List<string> accessions, string outFilePath)
        {
            SQLiteCommand _fmd;
            SQLiteConnection _connect;
            SQLiteDataReader _read;
            string org;
            short chargeState;
            string sequence;
            string accession;


            using (_connect = new SQLiteConnection("Datasource="+_dbPath+";Version=3;"))
            {
                _connect.Open();
                using (_fmd = _connect.CreateCommand())
                {
                    _fmd.CommandText = ("SELECT * " +
                                        "FROM peptide "+ 
                                        "WHERE refseq_id in (" + string.Join(", ", accessions) + ")");
                    _fmd.CommandType = CommandType.Text;
                    _read = _fmd.ExecuteReader();
                    using (StreamWriter writer = new StreamWriter(outFilePath))
                    {
                        while (_read.Read())
                        {
                            accession = (_read.GetString(0));
                            sequence = (_read.GetString(1));
                            chargeState = (_read.GetInt16(2));
                            writer.WriteLine(string.Format("ref|{1}{0}{2}{0}{3}", ',', accession, chargeState, sequence));
                        }
                    }
                    _connect.Close();
                }
            }
        }
    }
}
