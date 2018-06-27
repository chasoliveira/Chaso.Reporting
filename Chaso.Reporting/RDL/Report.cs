using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace Chaso.Reporting.RDL
{
    [Serializable(), XmlRoot("Report")]
    public class Report : SerializableBase
    {
        #region Serialization Interface
        public List<DataSource> DataSources = new List<DataSource>();
        public List<DataSet> DataSets = new List<DataSet>();
        public List<ReportParameter> ReportParameters = new List<ReportParameter>();
        public ReportSection ReportSections { get; set; }
        #endregion

        #region Factory Methods
        //override the constructor, so you don't have to cast it after deserializing it
        public new static Report Deserialize(string xml, Type type)
        {
            Report re;
            re = (Report)SerializableBase.Deserialize(xml, type);

            //copy the type-names from the ReportParameters to the QueryParameters
            re.ResolveParameterTypes();

            return re;
        }

        public static System.Xml.XmlNode GetNode(string xml, string node)
        {
            var xmlDoc = new System.Xml.XmlDocument();
            xmlDoc.LoadXml(xml);
            System.Xml.XmlNode bodyNode = xmlDoc.GetElementsByTagName(node).Item(0);

            return bodyNode ;
        }
        ///<summary>
        ///Gets a Report object based on the XML within the specified file
        ///</summary>
        public static Report GetReportFromFile(string reportFileName)
        {
            Report re = new Report();
            string xml;

            try
            {
                xml = System.IO.File.ReadAllText(reportFileName);
                re = Deserialize(xml, typeof(Report));
                re.ReportSections = ReportSection.NewFromXmlNode(GetNode(xml, "Body"));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Print(ex.Message);
                //ErrorHandling.ErrorLogger.LogException(ex, "ReportFileName=" & ReportFileName)
                throw ex;
            }

            return re;
        }
        #endregion

        #region Helper Methods
        /// <summary>
        /// map the report parameters to the query parameters
        /// </summary>
        private void ResolveParameterTypes()
        {
            foreach (ReportParameter rParam in this.ReportParameters)
            {
                foreach (DataSet ds in this.DataSets)
                {
                    foreach (QueryParameter qParam in ds.Query.QueryParameters)
                    {
                        if (qParam.Value == $"=Parameters!{ rParam.Name }.Value")
                        {
                            qParam.DataType = rParam.DataType;
                        }
                    }
                }
                rParam.SetUpValidValues(this.DataSets);
            }
        }
        public List<DataSet> GetDataSetsInReportSections()
        {
            return DataSets.Where(d => ReportSections.DataSetNames.Contains(d.Name)).ToList();
        }
        #endregion
    }
}
