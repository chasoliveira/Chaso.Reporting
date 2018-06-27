using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using System.Collections.Specialized;

namespace Chaso.Reporting.Test
{
    [TestClass]
    public class ReportEngineTest
    {
        [TestMethod]
        public void MustReturnAValidReport()
        {
            //Arrange
            var reportPath = $@"{AppDomain.CurrentDomain.BaseDirectory}\Reports";
            var reportFileName = "Sample.rdl";
            NameValueCollection querystring = new NameValueCollection
            {
                { "Date_Ini", "2017-10-01" }
            };
            
            IEngine builder = new Engine("\\", reportPath, reportFileName, querystring);
            builder.OnError += (s, e) =>
            {
                foreach (var item in e.ErrorMessages)
                {
                    Debug.WriteLine(item.Message, "Message");
                    Debug.WriteLine(item.StackTrace, "StackTrace");
                }
            };

            //Act
            var report = builder.ReportViewer();

            //Assert
            Assert.IsNotNull(report);
        }
    }
}
