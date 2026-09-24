using D365.Community.Ps.Automation.Contract.Content;
using System;
using System.Collections.Generic;
using System.Text;
using D365.Community.Ps.Automation.Service;
using System.Linq;
using System.Web;
using System.Threading;

namespace D365.Community.Ps.Automation.Job
{
    internal static class SavedQueryJob
    {
        internal static bool Execute(Contract.Config.SavedQueries queries)
        {
            var result = GetQueries(out var savedQueries);
            if (!result) return false;
            if (savedQueries.Value == null || savedQueries.Value.Count < 1) return true;
            foreach (var savedQuery in savedQueries.Value)
            {
                if (queries.DisabledOutlookTemplates.Contains(savedQuery.Name) && savedQuery.IsDefault == true)
                {
                    Console.WriteLine($"INFO: Deactivate OutlookTemplate {savedQuery.Name}");
                    var queryBody = new StringBuilder()
                        .OpenJson()
                        .AppendValue("isdefault", false)
                        .CloseJson();
                    result = Client.Patch($"{PsAutomation.ApiUrl}/savedqueries({savedQuery.SavedQueryId:D})", queryBody.ToString()) && result;
                }
                if (!queries.DisabledOutlookTemplates.Contains(savedQuery.Name) && savedQuery.IsDefault == false)
                {
                    Console.WriteLine($"INFO: Activate OutlookTemplate {savedQuery.Name}");
                    var queryBody = new StringBuilder()
                        .OpenJson()
                        .AppendValue("isdefault", true)
                        .CloseJson();
                    result = Client.Patch($"{PsAutomation.ApiUrl}/savedqueries({savedQuery.SavedQueryId:D})", queryBody.ToString()) && result;
                }
            }

            //anything to do?
            if (!queries.OutlookTemplates.Any())
            {
                return result;
            }

            foreach (var outlookTemplate in queries.OutlookTemplates)
            {
                var savedQuery = savedQueries.Value.FirstOrDefault(e => e.Name == outlookTemplate.Name);
                if (savedQuery == null)
                {
                    Console.WriteLine($"INFO: Create OutlookTemplate {outlookTemplate.Name}");
                    var templateBody = new StringBuilder()
                        .OpenJson()
                        .AppendValue("isquickfindquery", false)
                        .AppendValue("fetchxml", outlookTemplate.FetchXml)
                        .AppendValue("querytype", 131072)
                        .AppendValue("isdefault", outlookTemplate.IsDefault)
                        .AppendValue("returnedtypecode", outlookTemplate.Entity)
                        .AppendValue("name", outlookTemplate.Name)
                        .AppendValue("description", $"{outlookTemplate.Description} ({DateTime.UtcNow:yyyy-MM-dd HH:mm:ss})")
                        .CloseJson();
                    result = Client.Post($"{PsAutomation.ApiUrl}/savedqueries", templateBody.ToString(), out _) && result;
                }
                else
                {
                    var activeFetchXml = savedQuery.FetchXml;
                    var activeFetchXmlToQueryExpression = $"{PsAutomation.ApiUrl}/FetchXmlToQueryExpression(FetchXml=@p1)?@p1='{HttpUtility.UrlEncode(activeFetchXml)}'";
                    result = Client.Get<QueryExpression>(activeFetchXmlToQueryExpression, out var activeQueryExpression) && result;
                    //quirky extract, but ok for the use case
                    var activeJson = activeQueryExpression.Json?.Trim();

                    var expectedFetchXml = outlookTemplate.FetchXml;
                    var expectedFetchXmlToQueryExpression = $"{PsAutomation.ApiUrl}/FetchXmlToQueryExpression(FetchXml=@p1)?@p1='{HttpUtility.UrlEncode(expectedFetchXml)}'";
                    result = Client.Get<QueryExpression>(expectedFetchXmlToQueryExpression, out var expectedQueryExpression) && result;
                    var expectedJson = expectedQueryExpression.Json?.Trim();

                    if (!string.Equals(activeJson, expectedJson, StringComparison.InvariantCultureIgnoreCase))
                    {
                        Console.WriteLine($"INFO: Update OutlookTemplate {savedQuery.Name}");
                        Console.WriteLine("WARNING: Update OutlookTemplate does not affect existing rule for users!");
                        var queryBody = new StringBuilder()
                            .OpenJson()
                            .AppendValue("isdefault", false)
                            .CloseJson();
                        result = Client.Patch($"{PsAutomation.ApiUrl}/savedqueries({savedQuery.SavedQueryId:D})", queryBody.ToString()) && result;
                        Console.WriteLine($"INFO: Wait after 'update is default:false' {30000}sec");
                        Thread.Sleep(30000);
                        result = Client.Delete($"{PsAutomation.ApiUrl}/savedqueries({savedQuery.SavedQueryId:D})") && result;
                        Console.WriteLine($"INFO: Wait after 'delete' {30000}sec");
                        Thread.Sleep(30000);
                        var templateBody = new StringBuilder()
                            .OpenJson()
                            .AppendValue("isquickfindquery", false)
                            .AppendValue("fetchxml", outlookTemplate.FetchXml)
                            .AppendValue("querytype", 131072)
                            .AppendValue("isdefault", outlookTemplate.IsDefault)
                            .AppendValue("returnedtypecode", outlookTemplate.Entity)
                            .AppendValue("name", outlookTemplate.Name)
                            .AppendValue("description", $"{outlookTemplate.Description} ({DateTime.UtcNow:yyyy-MM-dd HH:mm:ss})")
                            .CloseJson();
                        result = Client.Post($"{PsAutomation.ApiUrl}/savedqueries", templateBody.ToString(), out _) && result;
                    }
                    else
                    {
                        Console.WriteLine($"INFO: Checked OutlookTemplate {outlookTemplate.Name}");
                    }
                }
            }

            return result;
        }


        private static bool GetQueries(out ODataContents<List<Contract.Content.SavedQuery>> content)
        {
            var savedqueries = $"{PsAutomation.ApiUrl}/savedqueries?$select=name,description,isdefault,fetchxml,returnedtypecode,statecode,statuscode&$filter={Uri.EscapeDataString("componentstate eq 0 and querytype eq 131072")}&$orderby={Uri.EscapeDataString("name asc")}";
            var result = Client.Fetch<ODataContents<List<Contract.Content.SavedQuery>>>(savedqueries, out var response);
            if (response.StatusCode == 200)
            {
                content = (ODataContents<List<Contract.Content.SavedQuery>>)response.Content;
            }
            else
            {
                content = new ODataContents<List<Contract.Content.SavedQuery>>();
            }
            
            return result;
        }
    }
}
