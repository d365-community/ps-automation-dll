using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using D365.Community.Ps.Automation.Contract.Converter;
using D365.Community.Ps.Automation.Service;

namespace D365.Community.Ps.Automation.Export
{
    internal static class AssemblyRegistrationExport
    {
        internal static bool Execute(string directory, string prefix, string filter)
        {
            var queryFilter = string.IsNullOrWhiteSpace(filter) ? "" : $"&$filter={Uri.EscapeDataString(filter)}";
            var pluginassemblies = $"{PsAutomation.ApiUrl}/pluginassemblies?$select=name,description{queryFilter}&$orderby={Uri.EscapeDataString("name asc")}&$expand=pluginassembly_plugintype($select=name,friendlyname,typename,workflowactivitygroupname,description,isworkflowactivity;$expand=plugintypeid_sdkmessageprocessingstep($select=name,mode,asyncautodelete,description,filteringattributes,rank,stage,configuration;$expand=sdkmessageprocessingstepid_sdkmessageprocessingstepimage($select=attributes,name,entityalias,imagetype)))";
            var result = Client.Fetch<Contract.Content.ODataContents<List<Contract.Content.PluginAssembly>>>(pluginassemblies, out var response);
            if (response.StatusCode != 200) return result;
            var content = (Contract.Content.ODataContents<List<Contract.Content.PluginAssembly>>)response.Content;
            if (content.Value == null || content.Value.Count < 1) return true;

            foreach (var pluginassembly in content.Value)
            {
                var assembly = new Contract.Assembly.PluginAssembly
                {
                    Assembly = pluginassembly.Name,
                    Description = pluginassembly.Description,
                    PluginTypes = new List<Contract.Assembly.PluginType>(pluginassembly.PluginTypes.Count)
                };
                pluginassembly.PluginTypes = pluginassembly.PluginTypes.OrderBy(o => o.Name).ToList();
                foreach (var pluginType in pluginassembly.PluginTypes)
                {
                    var type = new Contract.Assembly.PluginType
                    {
                        Plugin = pluginType.Name,
                        FriendlyName = pluginType.FriendlyName,
                        Description = pluginType.Description,
                        TypeName = pluginType.TypeName,
                        WorkflowActivityGroupName = pluginType.WorkflowActivityGroupName,
                        IsWorkflowActivity = pluginType.IsWorkflowActivity,
                        SdkMessageProcessingSteps = new List<Contract.Assembly.SdkMessageProcessingStep>(pluginType.SdkMessageProcessingSteps.Count)
                    };
                    pluginType.SdkMessageProcessingSteps = pluginType.SdkMessageProcessingSteps.OrderBy(o => o.Name).ToList();
                    foreach (var sdkMessageProcessingStep in pluginType.SdkMessageProcessingSteps)
                    {
                        var step = new Contract.Assembly.SdkMessageProcessingStep
                        {
                            Step = sdkMessageProcessingStep.Name,
                            Description = sdkMessageProcessingStep.Description,
                            ExecutionOrder = sdkMessageProcessingStep.Rank,
                            AsyncAutoDelete = sdkMessageProcessingStep.AsyncAutoDelete,
                            FilteringAttributes = CsvSorter.Transform(sdkMessageProcessingStep.FilteringAttributes),
                            Mode = EnumConverter.Convert(sdkMessageProcessingStep.Mode),
                            Stage = EnumConverter.Convert(sdkMessageProcessingStep.Stage),
                            UnsecureConfiguration = sdkMessageProcessingStep.Configuration,
                            SdkMessageProcessingStepImages = new List<Contract.Assembly.SdkMessageProcessingStepImage>()
                        };
                        sdkMessageProcessingStep.SdkMessageProcessingStepImages = sdkMessageProcessingStep.SdkMessageProcessingStepImages.OrderBy(o => o.Name).ToList();
                        foreach (var sdkMessageProcessingStepImage in sdkMessageProcessingStep.SdkMessageProcessingStepImages)
                        {
                            step.SdkMessageProcessingStepImages.Add(new Contract.Assembly.SdkMessageProcessingStepImage
                            {
                                Image = sdkMessageProcessingStepImage.Name,
                                EntityAlias = sdkMessageProcessingStepImage.EntityAlias,
                                Parameters = CsvSorter.Transform(sdkMessageProcessingStepImage.Attributes),
                                ImageType = EnumConverter.Convert(sdkMessageProcessingStepImage.ImageType)
                            });
                        }
                        type.SdkMessageProcessingSteps.Add(step);
                    }
                    assembly.PluginTypes.Add(type);
                }
                File.WriteAllText(Path.Combine(directory, $"{prefix}{pluginassembly.Name}.json"), Serializer.JsonSerialize(assembly));
            }
            return result;
        }
    }
}
