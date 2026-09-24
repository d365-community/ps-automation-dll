using D365.Community.Ps.Automation.Contract.Content;
using System;
using System.Collections.Generic;
using System.Text;
using D365.Community.Ps.Automation.Service;

namespace D365.Community.Ps.Automation.Job
{
    internal static class SecureConfigJob
    {
        // /api/data/v9.2/pluginassemblies?$select=name&$filter=name eq 'Microsoft.CDS.AdvancedAnalyticsInfra.Plugins'&$expand=pluginassembly_plugintype($select=name;$filter=name eq 'Microsoft.CDS.AdvancedAnalyticsInfra.Plugins.DeleteEntityAnalyticsConfigPlugin';$expand=plugintypeid_sdkmessageprocessingstep($select=name,_sdkmessageprocessingstepsecureconfigid_value;$filter=name eq 'Microsoft.CDS.AdvancedAnalyticsInfra.Plugins.DeleteEntityAnalyticsConfigPlugin: Create of entityanalyticsconfig'))
        internal static bool Execute(string assembly, string plugin, string step, string config)
        {
            var pluginassemblies = $"{PsAutomation.ApiUrl}/pluginassemblies?$select=name&$filter={Uri.EscapeDataString($"name eq '{assembly}'")}&$expand=pluginassembly_plugintype($select=name;$filter={Uri.EscapeDataString($"name eq '{plugin}'")};$expand=plugintypeid_sdkmessageprocessingstep($select=name,_sdkmessageprocessingstepsecureconfigid_value;$filter={Uri.EscapeDataString($"name eq '{step}'")})";
            var result = Client.Fetch<ODataContents<List<PluginAssembly>>>(pluginassemblies, out var response);
            if (response.StatusCode == 200)
            {
                var content = (ODataContents<List<PluginAssembly>>)response.Content;
                if (content.Value == null || content.Value.Count < 1) return true;

                if (content.Value.Count != 1)
                {
                    Console.Error.WriteLine("ERROR: query does not return a unique assembly");
                    return false;
                }

                if (content.Value[0].PluginTypes.Count != 1)
                {
                    Console.Error.WriteLine("ERROR: query does not return a unique plugin");
                    return false;
                }

                if (content.Value[0].PluginTypes[0].SdkMessageProcessingSteps.Count != 1)
                {
                    Console.Error.WriteLine("ERROR: query does not return a unique step");
                    return false;
                }

                var secureConfigId = content.Value[0].PluginTypes[0].SdkMessageProcessingSteps[0].SdkMessageProcessingStepSecureConfigId;

                if (string.IsNullOrEmpty(config) && secureConfigId != null)
                {
                    //delete
                    result = Client.Delete($"{PsAutomation.ApiUrl}/sdkmessageprocessingstepsecureconfigs({secureConfigId.Value:D})") && result;
                }
                else if (!string.IsNullOrEmpty(config) && secureConfigId == null)
                {
                    var secureConfigBody = new StringBuilder()
                        .OpenJson()
                        .AppendValue("secureconfig", config)
                        .CloseJson();
                    //create
                    result = Client.Post($"{PsAutomation.ApiUrl}/sdkmessageprocessingstepsecureconfigs", secureConfigBody.ToString(), out var id) && result;
                    var stepId = content.Value[0].PluginTypes[0].SdkMessageProcessingSteps[0].SdkMessageProcessingStepId;
                    var stepBody = new StringBuilder()
                        .OpenJson()
                        .AppendValue("_sdkmessageprocessingstepsecureconfigid_value", $"{id:D}")
                        .CloseJson();
                    result = Client.Patch($"{PsAutomation.ApiUrl}/sdkmessageprocessingsteps({stepId:D})", stepBody.ToString()) && result;
                }
                else if (!string.IsNullOrEmpty(config) && secureConfigId != null)
                {
                    //compare
                    result = Client.Get<SdkMessageProcessingStepSecureConfig>($"{PsAutomation.ApiUrl}/sdkmessageprocessingstepsecureconfigs({secureConfigId.Value:D})", out var secureConfig) && result;
                    if (secureConfig.StatusCode == 200)
                    {
                        var secureContent = (SdkMessageProcessingStepSecureConfig)response.Content;
                        if (!string.Equals(config, secureContent.SecureConfig, StringComparison.InvariantCulture))
                        {
                            var secureConfigBody = new StringBuilder()
                                .OpenJson()
                                .AppendValue("secureconfig", config)
                                .CloseJson();
                            result = Client.Patch($"{PsAutomation.ApiUrl}/sdkmessageprocessingstepsecureconfigs({secureConfigId.Value:D})", secureConfigBody.ToString()) && result;
                        }
                    }
                }
            }
            return result;
        }
    }
}
