using System;
using System.Xml.Serialization;

namespace Chaso.Reporting.RDL
{
    [Serializable()]
    public class DataSource
    {
        [XmlAttribute]
        public string Name { get; set; }
        public ConnectionPropertie ConnectionProperties { get; set; }
    }
}