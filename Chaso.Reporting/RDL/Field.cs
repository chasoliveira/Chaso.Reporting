using System;
using System.Xml.Serialization;

namespace Chaso.Reporting.RDL
{
    /// <summary>
    /// child of DataSet, must contains the field definitions
    /// </summary>
    [Serializable()]
    public class Field
    {
        [XmlAttribute]
        public string Name { get; set; }
        public string DataField { get; set; }
        public string TypeName { get; set; }
    }
}
