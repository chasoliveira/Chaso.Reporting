using System;
using System.Xml.Serialization;

namespace Chaso.Reporting.RDL
{
    [Serializable()]
    public class DataSetReference
    {
        public string DataSetName { get; set; }
        public string ValueField { get; set; }
        public string LabelField { get; set; }
        [XmlIgnore]
        public DataSet DataSet { get; set; }
    }
}
