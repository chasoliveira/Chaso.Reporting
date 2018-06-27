using Microsoft.Reporting.WebForms;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Chaso.Reporting.RDL
{
    [Serializable()]
    public class DataSet
    {
        [XmlAttribute]
        public string Name { get; set; }
        public Query Query = new Query();
        public List<Field> Fields = new List<Field>();

        /// <summary>
        /// copy the query parameters values (probably from the QueryString) into this report's parameters (in this object)
        /// </summary>
        /// <param name="webParameters"></param>
        public void AssignParameters(ReportParameterInfoCollection webParameters)
        {
            foreach (QueryParameter param in this.Query.QueryParameters)
            {
                string paramName = param.Name.Replace("@", "");
                //if this report param was passed as an arg to the report, then populate it
                if (webParameters[paramName] != null)
                    param.Value = webParameters[paramName].Values[0];
            }
        }

        /// <summary>
        /// Fills a DataTable from this report's query
        /// </summary>
        /// <param name="DBConnectionString">a working DB connection string</param>
        /// <returns>one DataTable</returns>
        public System.Data.DataTable GetDataTable(string DBConnectionString)
        {
            System.Data.DataTable dataTable = new System.Data.DataTable();
            //OLEDB parameters
            if (DBConnectionString.Contains("Provider="))
                using (System.Data.OleDb.OleDbDataAdapter da = new System.Data.OleDb.OleDbDataAdapter(this.Query.CommandText, DBConnectionString))
                {
                    if (this.Query.QueryParameters.Count > 0)
                    {
                        foreach (QueryParameter param in this.Query.QueryParameters)
                        {
                            string paramName = param.Name.Replace("@", "");
                            //OLEDB chokes on the @symbol, it prefers ? marks
                            da.SelectCommand.CommandText = da.SelectCommand.CommandText.Replace(param.Name, "?");

                            switch (param.DataType)
                            {
                                case "Text":
                                    da.SelectCommand.Parameters.Add(new System.Data.OleDb.OleDbParameter(paramName, System.Data.OleDb.OleDbType.VarWChar) { Value = param.Value });
                                    break;
                                case "Boolean":
                                    da.SelectCommand.Parameters.Add(new System.Data.OleDb.OleDbParameter(paramName, System.Data.OleDb.OleDbType.Boolean) { Value = param.Value });
                                    break;
                                case "DateTime":
                                    da.SelectCommand.Parameters.Add(new System.Data.OleDb.OleDbParameter(paramName, System.Data.OleDb.OleDbType.Date) { Value = param.Value });
                                    break;
                                case "Integer":
                                    da.SelectCommand.Parameters.Add(new System.Data.OleDb.OleDbParameter(paramName, System.Data.OleDb.OleDbType.Integer) { Value = param.Value });
                                    break;
                                case "Float":
                                    da.SelectCommand.Parameters.Add(new System.Data.OleDb.OleDbParameter(paramName, System.Data.OleDb.OleDbType.Decimal) { Value = param.Value });
                                    break;
                                default:
                                    da.SelectCommand.Parameters.Add(new System.Data.OleDb.OleDbParameter(paramName, param.Value));
                                    break;
                            }
                        }
                    }
                    da.Fill(dataTable);
                }
            else //Sql Client parameters
                using (System.Data.SqlClient.SqlDataAdapter da = new System.Data.SqlClient.SqlDataAdapter(this.Query.CommandText, DBConnectionString))
                {
                    foreach (QueryParameter param in this.Query.QueryParameters)
                    {
                        string paramName = param.Name.Replace("@", "");
                        dynamic currentValue = param.Value;
                        if (currentValue == null || currentValue.Equals($"=Parameters!{paramName}.Value"))
                            currentValue = DBNull.Value;

                        switch (param.DataType)
                        {
                            case "Text":
                                da.SelectCommand.Parameters.Add(new System.Data.SqlClient.SqlParameter(paramName, System.Data.SqlDbType.VarChar) { Value = currentValue});
                                break;
                            case "Boolean":
                                da.SelectCommand.Parameters.Add(new System.Data.SqlClient.SqlParameter(paramName, System.Data.SqlDbType.Bit) { Value = currentValue });
                                break;
                            case "DateTime":
                                da.SelectCommand.Parameters.Add(new System.Data.SqlClient.SqlParameter(paramName, System.Data.SqlDbType.DateTime) { Value = currentValue });
                                break;
                            case "Integer":
                                da.SelectCommand.Parameters.Add(new System.Data.SqlClient.SqlParameter(paramName, System.Data.SqlDbType.Int) { Value = currentValue });
                                break;
                            case "Float":
                                da.SelectCommand.Parameters.Add(new System.Data.SqlClient.SqlParameter(paramName, System.Data.SqlDbType.Decimal) { Value = currentValue });
                                break;
                            default:
                                 da.SelectCommand.Parameters.Add(new System.Data.SqlClient.SqlParameter(param.Name, currentValue)); 
                                break;
                        }
                    }
                    da.Fill(dataTable);
                }

            dataTable.TableName = this.Name;
            return dataTable;
        }
    }
}
