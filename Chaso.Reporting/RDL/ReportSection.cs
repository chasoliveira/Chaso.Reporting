using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;

namespace Chaso.Reporting.RDL
{
    public class ReportSection
    {
        [XmlIgnore]
        public IList<string> DataSetNames { get; set; }

        public static ReportSection NewFromXmlNode(XmlNode xmlNode)
        {
            var dataSetNames = new List<string>();
            XmlDocument xd = new XmlDocument();
            xd.LoadXml(xmlNode.OuterXml);

            var xmldsNames = xd.GetElementsByTagName("DataSetName");
            
            for (int i = 0; i < xmldsNames.Count; i++)
            {
                dataSetNames.Add(xmldsNames.Item(i).InnerText);
            }
            return new ReportSection() { DataSetNames = dataSetNames};
        }
    }
}
