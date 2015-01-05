using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BiodiversityPlugin.Models
{
    public class KeggKoInformation
    {
        private string m_keggKoId;
        private string m_keggGeneName;
        private string m_keggEc;

        public string KeggKoId
        {
            get { return m_keggKoId; }
            set { m_keggKoId = value; }
        }

        public string KeggGeneName
        {
            get { return m_keggGeneName; }
            set { m_keggGeneName = value; }
        }

        public string KeggEc
        {
            get { return m_keggEc; }
            set { m_keggEc = value; }
        }

        public KeggKoInformation(string id, string name, string ec)
        {
            KeggKoId = id;
            KeggGeneName = name;
            KeggEc = ec;
        }
    }
}
