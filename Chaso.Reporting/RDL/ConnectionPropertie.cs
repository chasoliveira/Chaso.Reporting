using System;

namespace Chaso.Reporting.RDL
{
    [Serializable()]
    public class ConnectionPropertie
    {
        public string DataProvider { get; set; }
        public string ConnectString { get; set; }
        public string Prompt { get; set; }
    }
}
