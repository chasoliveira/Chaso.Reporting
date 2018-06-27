using System;
using System.Xml.Serialization;

namespace Chaso.Reporting.RDL
{
    /// <summary>
    /// child of DataSet, must contais the parameter properties
    /// </summary>
    [Serializable()]
    public class QueryParameter
    {
        [XmlAttribute]
        public string Name { get; set; }
        public string DataType { get; set; }
        public string Value { get; set; }
    }
}
