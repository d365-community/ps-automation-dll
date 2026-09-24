using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using D365.Community.Ps.Automation.Contract.Content;
using D365.Community.Ps.Automation.Contract.Pac;
using D365.Community.Ps.Automation.Service;

namespace D365.Community.Ps.Automation.Export
{
    internal static class EnvironmentVariableExport
    {
        internal static bool Execute(string directory, string prefix, string filter)
        {
            var queryFilter = string.IsNullOrWhiteSpace(filter) ? "" : Uri.EscapeDataString($" and ({filter})");
            var environmentvariabledefinitions = $"{PsAutomation.ApiUrl}/environmentvariabledefinitions?$select=displayname,schemaname,defaultvalue,description&$filter={Uri.EscapeDataString("statecode eq 0")}{queryFilter}&$orderby={Uri.EscapeDataString("schemaname asc")}&$expand=environmentvariabledefinition_environmentvariablevalue($select=value;$filter={Uri.EscapeDataString("statecode eq 0")})";
            var result = Client.Fetch<ODataContents<List<EnvironmentVariableDefinition>>>(environmentvariabledefinitions, out var response);
            if (response.StatusCode != 200) return result;
            var content = (ODataContents<List<EnvironmentVariableDefinition>>)response.Content;
            if (content.Value == null || content.Value.Count < 1) return true;

            var pacSettings = new PacSettings
            {
                EnvironmentVariables = new List<EnvironmentVariable>()
            };

            foreach (var environmentvariabledefinition in content.Value)
            {
                pacSettings.EnvironmentVariables.Add(new EnvironmentVariable
                {
                    SchemaName = environmentvariabledefinition.SchemaName,
                    Value = environmentvariabledefinition.EnvironmentVariableValue.FirstOrDefault()?.Value ?? "",
                });
            }

            File.WriteAllText(Path.Combine(directory, $"{prefix}pac-environment-variable.json"), Serializer.JsonSerialize(pacSettings));
            return result;
        }
    }
}
