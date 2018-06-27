using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using System.Collections.Specialized;
using System.Data;
using Newtonsoft.Json;
using WebForms = Microsoft.Reporting.WebForms;

namespace Chaso.Reporting
{
    public class Engine : IEngine
    {
        public event EventHandler<EngineErrorEventArgs> OnError;
        private string BaseUrl;
        private string ReportName;
        private string ReportPath;
        private Hashtable _reportParameters { get; set; }
        private WebForms.ReportViewer CurrentReportViewer;
        private RDL.Report _reportDefinition;
        private FileInfo _reportfileInfo;
        private List<ErrorMessage> ErrorMessages;

        public Engine(string baseUrl, string reportPath, string reportName, NameValueCollection querystring)
        {
            this.BaseUrl = baseUrl;
            this.ReportPath = reportPath;
            this.ReportName = reportName;
            this.ErrorMessages = new List<ErrorMessage>();

            this.CurrentReportViewer = new WebForms.ReportViewer
            {
                ProcessingMode = WebForms.ProcessingMode.Local,
                SizeToReportContent = true
            };

            _reportParameters = new Hashtable();
            this.CurrentReportViewer.ReportError += CurrentReportViewer_ReportError;
            SetParameters(querystring);
        }

        public WebForms.ReportViewer ReportViewer()
        {
            FileInfo reportFullPath = this.ReportFile;
            //check to make sure the file ACTUALLY exists, before we start working on it
            if (reportFullPath != null)
            {
                //map the reporting engine to the .rdl/.rdlc file
                LoadReportDefinitionFile(CurrentReportViewer.LocalReport, reportFullPath);

                //  1. Clear Report Data
                CurrentReportViewer.LocalReport.DataSources.Clear();

                //  2. Load new data
                // Look-up the DB query in the "DataSets" element of the report file (.rdl/.rdlc which contains XML)
                RDL.Report reportDef = this.ReportDefinition;

                //Load any other report parameters (which are not part of the DB query).  
                //If any of the parameters are required, make sure they were provided, or show an error message.  Note: SSRS cannot render the report if required parameters are missing
                CheckReportParameters(CurrentReportViewer.LocalReport);

                if (!ErrorMessages.Any())
                    // Run each query (usually, there is only one) and attach each to the report
                    foreach (RDL.DataSet ds in reportDef.GetDataSetsInReportSections())
                    {
                        WebForms.ReportDataSource rds = GetReportDataSource(reportDef, ds);
                        CurrentReportViewer.LocalReport.DataSources.Add(rds);
                    }

                CurrentReportViewer.LocalReport.Refresh();
            }
            return CurrentReportViewer;
        }
        public List<RDL.ReportParameter> Parameters()
        {
            return ReportDefinition.ReportParameters;
        }
        /// <summary>
        /// Get data for specific parameter in specific datasource
        /// </summary>
        /// <param name="parameterName">parameter</param>
        /// <param name="dataSourceName">data source name</param>
        /// <returns>string as JSON with a list of elements</returns>
        public string GetDataForParameterAsJson(string parameterName, string dataSourceName)
        {
            return DataTableAsJson(GetData(parameterName, dataSourceName));
        }
        public T GetDataForParameter<T>(string parameterName, string dataSourceName)
        {
            return DataTableToJson<T>(GetData(parameterName, dataSourceName));
        }

        private DataTable GetData(string parameterName, string dataSourceName)
        {
            FileInfo reportFullPath = this.ReportFile;
            //check to make sure the file ACTUALLY exists, before we start working on it
            if (reportFullPath == null)
                return null;

            var errorMessages = new List<ErrorMessage>();
            //map the reporting engine to the .rdl/.rdlc file
            LoadReportDefinitionFile(CurrentReportViewer.LocalReport, reportFullPath);

            // Look-up the DB query in the "DataSets" element of the report file (.rdl/.rdlc which contains XML)
            RDL.Report reportDef = this.ReportDefinition;
            CheckReportParameters(CurrentReportViewer.LocalReport);

            var parameter = reportDef.ReportParameters.FirstOrDefault(p => p.Name.Equals(parameterName));
            if (parameter == null)
            {
                ReportSingleErroMessage($"The parameter {parameterName } was not found in report {ReportName}");
                return null;
            }
            // Run each query (usually, there is only one) and attach each to the report
            var validValue = parameter.ValidValues.FirstOrDefault(v => v.DataSetName.Equals(dataSourceName));
            if (validValue == null)
            {
                ReportSingleErroMessage($"The parameter {parameterName } has no valid DataSetName ({dataSourceName}) for report {ReportName}");
                return null;
            }
            RDL.DataSet ds = parameter.ValidValues.FirstOrDefault().DataSet;

            WebForms.ReportDataSource rds = GetReportDataSource(reportDef, ds);
            return (DataTable)rds.Value;
        }
        private void SetParameters(NameValueCollection queryString)
        {
            var excludes = new[] { "reportpath", "reportname", "_" };
            //gather any params so they can be passed to the report
            //ignore the “path” param. It describes the report's file path
            foreach (string key in queryString.AllKeys.Where(k => !excludes.Contains(k.ToLower())))
                _reportParameters.Add(key, queryString[key]);
        }

        private void CurrentReportViewer_ReportError(object sender, WebForms.ReportErrorEventArgs e)
        {
            System.Exception ex = e.Exception;
            var errorMessages = new List<ErrorMessage>();
            while (ex != null)
            {
                errorMessages.Add(new ErrorMessage(ex.Message, ex.StackTrace));
                ex = ex.InnerException;
            }
            OnError?.Invoke(this, new EngineErrorEventArgs(errorMessages));
        }

        private FileInfo ReportFile
        {
            get
            {
                try
                {
                    string reportFullPath = "";


                    //check to make sure the file ACTUALLY exists, before we start working on it
                    if (File.Exists(Path.Combine(this.ReportPath, ReportName)))
                    {
                        reportFullPath = Path.Combine(this.ReportPath, ReportName);
                        ReportName = ReportName.Substring(0, ReportName.LastIndexOf("."));
                    }
                    else if (File.Exists(Path.Combine(this.ReportPath, $"{ ReportName }.rdl")))
                        reportFullPath = Path.Combine(this.ReportPath, $"{ ReportName }.rdl");
                    else if (File.Exists(Path.Combine(this.ReportPath, $"{ ReportName }.rdlc")))
                        reportFullPath = Path.Combine(this.ReportPath, $"{ ReportName }.rdlc");

                    if (reportFullPath != "")
                        return _reportfileInfo ?? (_reportfileInfo = new FileInfo(reportFullPath));
                }
                finally { }
                return null;
            }
        }

        /// <summary>
        /// the Report file (.rdl/.rdlc) de-serialized into an object
        /// </summary>
        private RDL.Report ReportDefinition
        {
            get
            {
                if (_reportDefinition == null)
                {
                    FileInfo reportFile = this.ReportFile;
                    if (reportFile != null)
                        _reportDefinition = RDL.Report.GetReportFromFile(reportFile.FullName);
                    else
                        _reportDefinition = new RDL.Report();
                }

                return _reportDefinition;
            }
        }

        /// <summary>
        /// Note: SSRS cannot render the report if required parameters are missing.
        /// This will load any report parameters.
        /// If any of the parameters were required, but they were not provided, show an error message.
        /// </summary>
        /// <param name="report">Instance of the ReportViewer control or equiv object</param>
        private void CheckReportParameters(WebForms.LocalReport report)
        {
            //copy-in any report parameters which were not part of the DB query
            try
            {
                WebForms.ReportParameterInfoCollection rdlParams = report.GetParameters();
                foreach (WebForms.ReportParameterInfo rdlParam in rdlParams)
                {
                    if (this._reportParameters.ContainsKey(rdlParam.Name))
                    {
                        string val = this._reportParameters[rdlParam.Name].ToString();
                        if (string.IsNullOrEmpty(val))
                        {
                            if (rdlParam.Nullable)
                            {
                                val = null;
                                report.SetParameters(new WebForms.ReportParameter(rdlParam.Name, val, false));
                            }
                            else
                                ErrorMessages.Add(new ErrorMessage("Erro on Report Parameters.", $"Report Parameter value \"{ rdlParam.Name }\" is required, but was not provided."));
                        }
                        else
                            report.SetParameters(new WebForms.ReportParameter(rdlParam.Name, val));
                    }
                    else if (!rdlParam.AllowBlank)
                    {
                        ErrorMessages.Add(new ErrorMessage("Erro on Report Parameters.", $"Report Parameter \"{ rdlParam.Name }\" is required, but was not provided."));
                    }
                }
                if (ErrorMessages.Any())
                    OnError?.Invoke(this, new EngineErrorEventArgs(ErrorMessages));
            }
            catch (WebForms.LocalProcessingException ex)
            {
                ErrorMessages.Add(new ErrorMessage(ex.Message, ex.StackTrace));
                Exception exIn = ex.InnerException;
                while (exIn != null)
                {
                    ErrorMessages.Add(new ErrorMessage(exIn.Message, exIn.StackTrace));
                    exIn = exIn.InnerException;
                }

                OnError?.Invoke(this, new EngineErrorEventArgs(ErrorMessages));
            }
        }

        /// <summary>
        /// Load the .rdl/.rdlc file into the reporting engine.  Also, fix the path for any embedded graphics.
        /// </summary>
        /// <param name="report">Instance of the ReportViewer control or equiv object</param>
        /// <param name="reportFullPath">(file) path to the Reports folder (on the HDD)</param>
        private void LoadReportDefinitionFile(WebForms.LocalReport report, FileInfo reportFullPath)
        {
            string xml = File.ReadAllText(reportFullPath.FullName);
            if (xml.Contains("<Image"))
                report.EnableExternalImages = true;
            if (xml.Contains("<Hyperlink"))
                report.EnableHyperlinks = true;

            report.ReportPath = reportFullPath.FullName;
            report.SetBasePermissionsForSandboxAppDomain(new System.Security.PermissionSet(System.Security.Permissions.PermissionState.Unrestricted));
        }

        private void ReportSingleErroMessage(string message)
        {
            OnError?.Invoke(this, new EngineErrorEventArgs(
                                    new List<ErrorMessage>() { new ErrorMessage(message) }));
        }

        private WebForms.ReportDataSource GetReportDataSource(RDL.Report reportDef, RDL.DataSet ds)
        {
            //copy the parameters from the QueryString into the ReportParameters definitions (objects)
            var parameters = CurrentReportViewer.LocalReport.GetParameters();
            ds.AssignParameters(parameters);

            var dataSource = reportDef.DataSources.Find(d => d.Name == ds.Query.DataSourceName);

            //run the query to get real data for the report
            System.Data.DataTable tbl = ds.GetDataTable(dataSource.ConnectionProperties.ConnectString);

            //attach the data/table to the Report's dataset(s), by name
            //This refers to the dataset name in the RDLC file
            return new WebForms.ReportDataSource
            {
                Name = ds.Name,
                Value = tbl
            };
        }

        private static T DataTableToJson<T>(DataTable dataTable)
        {
            string jsonString = DataTableAsJson(dataTable);
            return JsonConvert.DeserializeObject<T>(jsonString);
        }
        private static string DataTableAsJson(DataTable dataTable)
        {
            var rows = (from DataRow d in dataTable.Rows
                        select dataTable.Columns.Cast<DataColumn>().ToDictionary(col => col.ColumnName, col => d[col])).ToList();

            var jsonString = JsonConvert.SerializeObject(rows);
            return jsonString;
        }
    }
}
