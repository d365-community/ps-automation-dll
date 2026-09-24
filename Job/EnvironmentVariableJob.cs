using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using D365.Community.Ps.Automation.Contract.Content;
using D365.Community.Ps.Automation.Contract.Pac;
using D365.Community.Ps.Automation.Service;

namespace D365.Community.Ps.Automation.Job
{
    internal static class EnvironmentVariableJob
    {
        internal static bool Execute(string directory, string prefix, string file)
        {
            var result = true;
            var path = Path.Combine(directory.TrimEnd('\\'), $"{prefix}{file}.json");
            var pacSettings = Serializer.JsonDeserialize<PacSettings>(File.ReadAllText(path));

            foreach (var environmentVariable in pacSettings.EnvironmentVariables)
            {
                var filter = Uri.EscapeDataString($" and schemaname eq '{environmentVariable.SchemaName}'");
                var environmentvariabledefinitions = $"{PsAutomation.ApiUrl}/environmentvariabledefinitions?$select=displayname,schemaname,defaultvalue,description&$filter={Uri.EscapeDataString("statecode eq 0")}{filter}&$orderby={Uri.EscapeDataString("schemaname asc")}&$expand=environmentvariabledefinition_environmentvariablevalue($select=value;$filter={Uri.EscapeDataString("statecode eq 0")})";
                result = Client.Fetch<ODataContents<List<EnvironmentVariableDefinition>>>(environmentvariabledefinitions, out var response) && result;
                if (response.StatusCode == 200)
                {
                    var content = (ODataContents<List<EnvironmentVariableDefinition>>)response.Content;
                    if (content.Value == null || content.Value.Count != 1) continue;

                    var environmentvariabledefinition = content.Value.First();
                    var targetValue = environmentVariable.Value;

                    //env var has a value
                    if (environmentvariabledefinition.EnvironmentVariableValue.Any())
                    {
                        var currentValue = environmentvariabledefinition.EnvironmentVariableValue.First();
                        var uri = $"{PsAutomation.ApiUrl}/environmentvariablevalues({currentValue.EnvironmentVariableValueId:D})";
                        if (!string.IsNullOrEmpty(targetValue) && $"{currentValue.Value}" != $"{targetValue}")
                        {
                            //update
                            result = Client.Patch(uri, "{ 'value':'" + targetValue + "' }") && result;
                        }
                        else if (string.IsNullOrEmpty(targetValue))
                        {
                            //delete
                            result = Client.Delete(uri) && result;
                        }
                    }
                    else
                    {
                        var uri = $"{PsAutomation.ApiUrl}/environmentvariablevalues";
                        if (!string.IsNullOrEmpty(targetValue))
                        {
                            //create
                            result = Client.Post(uri, "{ 'environmentvariabledefinitionid@odata.bind':'environmentvariabledefinitions(" + environmentvariabledefinition.EnvironmentVariableDefinitionId.ToString("D") + ")', 'value':'" + targetValue + "' }", out _) && result;
                        }
                    }
                }
            }
            return result;
        }
    }
}
