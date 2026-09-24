using System;
using System.Collections.Generic;
using System.IO;
using D365.Community.Ps.Automation.Contract.Content;
using D365.Community.Ps.Automation.Contract.Pac;
using D365.Community.Ps.Automation.Service;

namespace D365.Community.Ps.Automation.Export
{
    internal static class ConnectionReferenceExport
    {
        internal static bool Execute(string directory, string prefix, string filter)
        {
            var queryFilter = string.IsNullOrWhiteSpace(filter) ? "" : Uri.EscapeDataString($" and ({filter})");
            var connectionreferences = $"{PsAutomation.ApiUrl}/connectionreferences?$select=connectionreferencedisplayname,connectionreferenceid,connectionreferencelogicalname,connectorid&$filter={Uri.EscapeDataString("statecode eq 0")}{queryFilter}&$orderby={Uri.EscapeDataString("connectionreferencelogicalname asc")}";
            var result = Client.Fetch<ODataContents<List<Contract.Content.ConnectionReference>>>(connectionreferences, out var response);
            if (response.StatusCode != 200) return result;
            var content = (ODataContents<List<Contract.Content.ConnectionReference>>)response.Content;
            if (content.Value == null || content.Value.Count < 1) return true;

            var pacSettings = new PacSettings
            {
                ConnectionReferences = new List<Contract.Pac.ConnectionReference>()
            };

            foreach (var connectionreference in content.Value)
            {
                pacSettings.ConnectionReferences.Add(new Contract.Pac.ConnectionReference
                {
                    LogicalName = connectionreference.ConnectionReferenceLogicalName,
                    ConnectionId = connectionreference.ConnectionReferenceId.ToString("D"),
                    ConnectorId = connectionreference.ConnectorId
                });
            }

            File.WriteAllText(Path.Combine(directory, $"{prefix}pac-connection-reference.json"), Serializer.JsonSerialize(pacSettings));
            return result;
        }
    }
}
