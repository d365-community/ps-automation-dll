using D365.Community.Ps.Automation.Contract.Content;
using System.Collections.Generic;
using System;
using D365.Community.Ps.Automation.Service;
using System.IO;
using D365.Community.Ps.Automation.Contract.Config;

namespace D365.Community.Ps.Automation.Export
{
    internal static class SavedQueryExport
    {
        internal static bool Execute(string directory, string prefix, string filter)
        {
            var queryFilter = string.IsNullOrWhiteSpace(filter) ? "" : Uri.EscapeDataString($" and ({filter})");
            var savedqueries = $"{PsAutomation.ApiUrl}/savedqueries?$select=name,description,isdefault,fetchxml,returnedtypecode,statecode,statuscode&$filter={Uri.EscapeDataString("componentstate eq 0 and querytype eq 131072")}{queryFilter}&$orderby={Uri.EscapeDataString("name asc")}";
            var result = Client.Fetch<ODataContents<List<SavedQuery>>>(savedqueries, out var response);
            if (response.StatusCode != 200) return result;
            var content = (ODataContents<List<SavedQuery>>)response.Content;
            if (content.Value == null || content.Value.Count < 1) return true;
            //var queries = content.Value;

            var queries = new SavedQueries
            {
                CallerObjectId = PsAutomation.CallerObjectId,
                MsCrmCallerId = PsAutomation.MsCrmCallerId,
                Filter = filter
            };

            foreach (var query in content.Value)
            {
                if (query.IsDefault)
                {
                    queries.OutlookTemplates.Add(new OutlookTemplate
                    {
                        Name = query.Name,
                        Description = query.Description,
                        Entity = query.ReturnedTypeCode,
                        FetchXml = query.FetchXml,
                        IsDefault = query.IsDefault,
                        StateCode = query.StateCode
                    });
                }
                else
                {
                    queries.DisabledOutlookTemplates.Add(query.Name);
                }
            }

            File.WriteAllText(Path.Combine(directory, $"{prefix}saved-query.json"), Serializer.JsonSerialize(queries));

            return result;
        }
    }
}
