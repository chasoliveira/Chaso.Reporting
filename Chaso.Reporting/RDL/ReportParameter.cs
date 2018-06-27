using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Chaso.Reporting.RDL
{
    /// <summary>
    /// child of Report
    /// </summary>
    [Serializable()]
    public class ReportParameter
    {
        [XmlAttribute]
        public string Name { get; set; }
        public string DataType { get; set; }
        public string Prompt { get; set; }
        public bool Nullable { get; set; }
        [XmlIgnore]
        public ParameterDataType Type
        {
            get
            {
                switch (DataType)
                {
                    case nameof(ParameterDataType.Boolean): return ParameterDataType.Boolean;
                    case nameof(ParameterDataType.DateTime): return ParameterDataType.DateTime;
                    case nameof(ParameterDataType.Float): return ParameterDataType.Float;
                    case nameof(ParameterDataType.Integer): return ParameterDataType.Integer;
                    case nameof(ParameterDataType.String):
                    default:
                        return ParameterDataType.String;
                }
            }
        }
        public List<DataSetReference> ValidValues = new List<DataSetReference>();

        internal void SetUpValidValues(List<DataSet> dataSets)
        {
            foreach (var ds in dataSets)
                foreach (var vv in ValidValues)
                    if (ds.Name == vv.DataSetName)
                        vv.DataSet = ds;
        }
    }

    public enum ParameterDataType
    {
        Boolean = 0,
        DateTime = 1,
        Float = 2,
        Integer = 3,
        String = 4
    }
}
