using System;
using Microsoft.Reporting.WebForms;
using System.Collections.Generic;

namespace Chaso.Reporting
{
    public interface IEngine
    {
        event EventHandler<EngineErrorEventArgs> OnError;

        ReportViewer ReportViewer();
        List<RDL.ReportParameter> Parameters();
        string GetDataForParameterAsJson(string parameterName, string dataSourceName);
        T GetDataForParameter<T>(string parameterName, string dataSourceName);
    }
}