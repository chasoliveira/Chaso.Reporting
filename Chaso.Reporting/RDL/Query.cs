using System;
using System.Collections.Generic;

namespace Chaso.Reporting.RDL
{
    /// <summary>
    /// child of DataSet, must contains the command text and data source name
    /// </summary>
    [Serializable()]
    public class Query
    {
        public string DataSourceName { get; set; }
        public List<QueryParameter> QueryParameters = new List<QueryParameter>();
        public string CommandText { get; set; }
    }
}
