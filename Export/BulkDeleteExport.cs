using System;
using System.Collections.Generic;
using System.IO;
using System.Web;
using D365.Community.Ps.Automation.Contract.Config;
using D365.Community.Ps.Automation.Contract.Content;
using D365.Community.Ps.Automation.Service;

namespace D365.Community.Ps.Automation.Export
{
    internal static class BulkDeleteExport
    {
        internal static bool Execute(string directory, string prefix, string filter)
        {
            var queryFilter = string.IsNullOrWhiteSpace(filter) ? "" : Uri.EscapeDataString($" and ({filter})");
            var asyncoperations = $"{PsAutomation.ApiUrl}/asyncoperations?$select=name,data,recurrencepattern,recurrencestarttime&$filter={Uri.EscapeDataString("operationtype eq 13 and recurrencestarttime ne null and data ne null")}{queryFilter}&$orderby={Uri.EscapeDataString("name asc")}";
            var result = Client.Fetch<ODataContents<List<AsyncOperation>>>(asyncoperations, out var response);
            if (response.StatusCode != 200) return result;
            var content = (ODataContents<List<AsyncOperation>>)response.Content;
            if (content.Value == null || content.Value.Count < 1) return true;

            var bulkDeletes = new BulkDeletes
            {
                CallerObjectId = PsAutomation.CallerObjectId,
                MsCrmCallerId = PsAutomation.MsCrmCallerId,
                Filter = filter
            };

            foreach (var operation in content.Value)
            {
                var data = operation.Data;
                //TODO: implement new style bulk delete 
                if(string.IsNullOrWhiteSpace(data)) continue;
                var start = data.IndexOf("<string>&lt;fetch", StringComparison.InvariantCultureIgnoreCase) + 8;
                var end = data.IndexOf("fetch&gt;</string>", StringComparison.InvariantCultureIgnoreCase) + 9;
                var fetchXml = data.Substring(start, end - start).Replace("&lt;", "<").Replace("&gt;", ">");
                var fetchXmlToQueryExpression = $"{PsAutomation.ApiUrl}/FetchXmlToQueryExpression(FetchXml=@p1)?@p1='{HttpUtility.UrlEncode(fetchXml)}'";
                result = Client.Get<QueryExpression>(fetchXmlToQueryExpression, out var queryExpression) && result;
                if (queryExpression.StatusCode == 200)
                {
                    if (queryExpression.Content == null) return false;
                    var query = ((QueryExpression)queryExpression.Content).Query;
                    var bulkDelete = new BulkDelete
                    {
                        Name = operation.Name,
                        RecurrencePattern = operation.RecurrencePattern,
                        RecurrenceStartTime = $"{operation.RecurrenceStartTime:HH:mm}",
                        //FetchXml = fetchXml,
                        QueryExpression = Serializer.JsonSerialize(query, false)
                    };
                    bulkDeletes.Deletes.Add(bulkDelete);
                }
            }
            File.WriteAllText(Path.Combine(directory, $"{prefix}bulk-delete.json"), Serializer.JsonSerialize(bulkDeletes));
            return result;
        }
    }
}
