using D365.Community.Ps.Automation.Service;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using D365.Community.Ps.Automation.Contract.Content;
using D365.Community.Ps.Automation.Contract.Converter;

namespace D365.Community.Ps.Automation.Job
{
    internal static class AssemblyRegistrationJob
    {
        internal static bool Execute(string directory, string prefix, params string[] assemblyNames)
        {
            var result = true;
            foreach (var assemblyName in assemblyNames)
            {
                var path = Path.Combine(directory.TrimEnd('\\'), $"{prefix}{assemblyName}.json");
                var jsonAssembly = Serializer.JsonDeserialize<List<Contract.Assembly.PluginAssembly>>(File.ReadAllText(path)).First();
                var pluginassemblies = $"{PsAutomation.ApiUrl}/pluginassemblies?$select=name,description&$filter={Uri.EscapeDataString($"name eq '{assemblyName}' and ismanaged ne true")}&$orderby={Uri.EscapeDataString("name asc")}&$expand=pluginassembly_plugintype($select=name,friendlyname,typename,workflowactivitygroupname,description,isworkflowactivity;$expand=plugintypeid_sdkmessageprocessingstep($select=name,mode,asyncautodelete,description,filteringattributes,rank,stage,configuration;$expand=sdkmessageprocessingstepid_sdkmessageprocessingstepimage($select=attributes,name,description,entityalias,imagetype)))";
                result = Client.Fetch<ODataContents<List<Contract.Content.PluginAssembly>>>(pluginassemblies, out var response) && result;
                if (response.StatusCode == 200)
                {
                    var content = (ODataContents<List<Contract.Content.PluginAssembly>>)response.Content;
                    if (content.Value == null || content.Value.Count != 1) continue;
                    var ceAssembly = content.Value.First();
                    //existing
                    foreach (var ceType in ceAssembly.PluginTypes)
                    {
                        //if (type.IsWorkflowActivity) continue;
                        var jsonType = jsonAssembly.PluginTypes.FirstOrDefault(t => t.Plugin == ceType.Name);
                        if (jsonType == null)
                        {
                            //delete
                            result = Client.Delete($"{PsAutomation.ApiUrl}/plugintypes({ceType.PluginTypeId:D})") && result;
                        }
                        else
                        {
                            //update
                            var updateType = false;
                            var typeBody = new StringBuilder().OpenJson();
                            if (StringExtensions.IsChanged(ceType.Description, jsonType.Description))
                            {
                                updateType = true;
                                typeBody.AppendValue("description", jsonType.Description);
                            }
                            if (StringExtensions.IsChanged(ceType.FriendlyName, jsonType.FriendlyName))
                            {
                                updateType = true;
                                typeBody.AppendValue("friendlyname", jsonType.FriendlyName);
                            }
                            if (StringExtensions.IsChanged(ceType.TypeName, jsonType.TypeName))
                            {
                                updateType = true;
                                typeBody.AppendValue("typename", jsonType.TypeName);
                            }
                            if (ceType.IsWorkflowActivity && StringExtensions.IsChanged(ceType.WorkflowActivityGroupName, jsonType.WorkflowActivityGroupName))
                            {
                                updateType = true;
                                typeBody.AppendValue("workflowactivitygroupname", jsonType.WorkflowActivityGroupName);
                            }
                            if (updateType)
                            {
                                typeBody.CloseJson();
                                //update
                                result = Client.Patch($"{PsAutomation.ApiUrl}/plugintypes({ceType.PluginTypeId:D})", typeBody.ToString()) && result;
                            }
                            //existing
                            foreach (var ceStep in ceType.SdkMessageProcessingSteps)
                            {
                                var step = ceStep.Name.Replace($"{ceType.Name}: ", "");
                                var jsonStep = jsonType.SdkMessageProcessingSteps.FirstOrDefault(s => s.Step == step);
                                if (jsonStep == null)
                                {
                                    //delete
                                    result = Client.Delete($"{PsAutomation.ApiUrl}/sdkmessageprocessingsteps({ceStep.SdkMessageProcessingStepId:D})") && result;
                                }
                                else
                                {
                                    //update
                                    var updateStep = false;
                                    var stepBody = new StringBuilder().OpenJson();
                                    if (jsonStep.AsyncAutoDelete != ceStep.AsyncAutoDelete)
                                    {
                                        updateStep = true;
                                        stepBody.AppendValue("asyncautodelete", jsonStep.AsyncAutoDelete);
                                    }
                                    if (StringExtensions.IsChanged(EnumConverter.Convert(ceStep.Stage), jsonStep.Stage))
                                    {
                                        updateStep = true;
                                        stepBody.AppendValue("stage", (int)EnumConverter.Convert<SdkMessageProcessingStepStage>(jsonStep.Stage));
                                    }
                                    if (StringExtensions.IsChanged(EnumConverter.Convert(ceStep.Mode), jsonStep.Mode))
                                    {
                                        updateStep = true;
                                        stepBody.AppendValue("mode", (int)EnumConverter.Convert<SdkMessageProcessingStepMode>(jsonStep.Mode));
                                    }
                                    if (StringExtensions.IsChanged(ceStep.Description, jsonStep.Description))
                                    {
                                        updateStep = true;
                                        stepBody.AppendValue("description", jsonStep.Description);
                                    }
                                    if (StringExtensions.IsChanged(ceStep.Configuration, jsonStep.UnsecureConfiguration))
                                    {
                                        updateStep = true;
                                        stepBody.AppendValue("configuration", jsonStep.UnsecureConfiguration);
                                    }
                                    if (jsonStep.ExecutionOrder != ceStep.Rank)
                                    {
                                        updateStep = true;
                                        stepBody.AppendValue("rank", jsonStep.ExecutionOrder);
                                    }
                                    if (StringExtensions.IsChanged(CsvSorter.Transform(ceStep.FilteringAttributes), CsvSorter.Transform(jsonStep.FilteringAttributes)))
                                    {
                                        updateStep = true;
                                        stepBody.AppendValue("filteringattributes", jsonStep.FilteringAttributes);
                                    }
                                    if (updateStep)
                                    {
                                        stepBody.CloseJson();
                                        //update
                                        result = Client.Patch($"{PsAutomation.ApiUrl}/sdkmessageprocessingsteps({ceStep.SdkMessageProcessingStepId:D})", stepBody.ToString()) && result;
                                    }
                                    //existing
                                    foreach (var ceImage in ceStep.SdkMessageProcessingStepImages)
                                    {
                                        var jsonImage = jsonStep.SdkMessageProcessingStepImages.FirstOrDefault(i => i.Image == ceImage.Name);
                                        if (jsonImage == null)
                                        {
                                            //delete
                                            result = Client.Delete($"{PsAutomation.ApiUrl}/sdkmessageprocessingstepimages({ceImage.SdkMessageProcessingStepImageId:D})") && result;
                                        }
                                        else
                                        {
                                            //update
                                            var updateImage = false;
                                            var imageBody = new StringBuilder().OpenJson();
                                            if (StringExtensions.IsChanged(ceImage.EntityAlias, jsonImage.EntityAlias))
                                            {
                                                updateImage = true;
                                                imageBody.AppendValue("entityalias", jsonImage.EntityAlias);
                                            }
                                            if (StringExtensions.IsChanged(EnumConverter.Convert(ceImage.ImageType), jsonImage.ImageType))
                                            {
                                                updateImage = true;
                                                imageBody.AppendValue("imagetype", (int)EnumConverter.Convert<SdkMessageProcessingStepImageImageType>(jsonImage.ImageType));
                                            }
                                            if (StringExtensions.IsChanged(CsvSorter.Transform(ceImage.Attributes), CsvSorter.Transform(jsonImage.Parameters)))
                                            {
                                                updateImage = true;
                                                imageBody.AppendValue("attributes", jsonImage.Parameters);
                                            }
                                            if (updateImage)
                                            {
                                                imageBody.CloseJson();
                                                //update
                                                result = Client.Patch($"{PsAutomation.ApiUrl}/sdkmessageprocessingstepimages({ceImage.SdkMessageProcessingStepImageId:D})", imageBody.ToString()) && result;
                                            }
                                        }
                                    }
                                    //missing
                                    result = CreateImages(jsonStep.SdkMessageProcessingStepImages.FindAll(i1 => !ceStep.SdkMessageProcessingStepImages.Exists(i2 => i2.Name == i1.Image)), ceStep.SdkMessageProcessingStepId) && result;
                                }
                            }
                            //missing
                            result = CreateSteps(jsonType.SdkMessageProcessingSteps.FindAll(s1 => !ceType.SdkMessageProcessingSteps.Exists(s2 => s2.Name == s1.Step)), ceType.Name, ceType.PluginTypeId) && result;
                        }
                    }
                    //missing
                    result = CreateTypes(jsonAssembly.PluginTypes.FindAll(t1 => !ceAssembly.PluginTypes.Exists(t2 => t2.TypeName == t1.TypeName)), ceAssembly.PluginAssemblyId) && result;
                }
            }
            return result;
        }

        private static bool CreateTypes(List<Contract.Assembly.PluginType> jsonTypes, Guid ceAssemblyId)
        {
            var result = true;
            foreach (var jsonType in jsonTypes)
            {
                var typeBody = new StringBuilder().OpenJson();
                typeBody
                    .AppendValue("pluginassemblyid@odata.bind", $"pluginassemblies({ceAssemblyId:D})")
                    .AppendValue("name", jsonType.Plugin)
                    .AppendValue("friendlyname", jsonType.FriendlyName)
                    .AppendValue("typename", jsonType.TypeName);
                if (jsonType.IsWorkflowActivity)
                {
                    typeBody.AppendValue("workflowactivitygroupname", jsonType.WorkflowActivityGroupName);
                }
                typeBody.CloseJson();
                //create
                result = Client.Post($"{PsAutomation.ApiUrl}/plugintypes", typeBody.ToString(), out var ceTypeId) && result;
                result = CreateSteps(jsonType.SdkMessageProcessingSteps, jsonType.Plugin, ceTypeId) && result;
            }
            return result;
        }

        private static bool CreateSteps(List<Contract.Assembly.SdkMessageProcessingStep> jsonSteps, string plugin, Guid ceTypeId)
        {
            var result = true;
            foreach (var jsonStep in jsonSteps)
            {
                var stepBody = new StringBuilder().OpenJson();
                stepBody
                    .AppendValue("asyncautodelete", jsonStep.AsyncAutoDelete)
                    .AppendValue("configuration", jsonStep.UnsecureConfiguration)
                    .AppendValue("description", jsonStep.Description)
                    .AppendValue("filteringattributes", jsonStep.FilteringAttributes)
                    .AppendValue("mode", (int)EnumConverter.Convert<SdkMessageProcessingStepMode>(jsonStep.Mode))
                    .AppendValue("stage", (int)EnumConverter.Convert<SdkMessageProcessingStepStage>(jsonStep.Stage))
                    .AppendValue("name", $"{plugin}: {jsonStep.Step}")
                    .AppendValue("rank", jsonStep.ExecutionOrder)
                    .AppendValue("plugintypeid@odata.bind", $"plugintypes({ceTypeId:D})");
                stepBody.CloseJson();
                //create
                result = Client.Post($"{PsAutomation.ApiUrl}/sdkmessageprocessingsteps", stepBody.ToString(), out var ceStepId) && result;
                result = CreateImages(jsonStep.SdkMessageProcessingStepImages, ceStepId) && result;
            }
            return result;
        }

        private static bool CreateImages(List<Contract.Assembly.SdkMessageProcessingStepImage> jsonImages, Guid ceStepId)
        {
            var result = true;
            foreach (var jsonImage in jsonImages)
            {
                var imageBody = new StringBuilder().OpenJson();
                imageBody
                    .AppendValue("attributes", jsonImage.Parameters)
                    .AppendValue("entityalias", jsonImage.EntityAlias)
                    .AppendValue("imagetype", (int)EnumConverter.Convert<SdkMessageProcessingStepImageImageType>(jsonImage.ImageType))
                    .AppendValue("name", jsonImage.Image)
                    .AppendValue("sdkmessageprocessingstepid@odata.bind", $"plugintypes({ceStepId:D})");
                imageBody.CloseJson();
                //create
                result = Client.Post($"{PsAutomation.ApiUrl}/sdkmessageprocessingstepimages", imageBody.ToString(), out _) && result;
            }
            return result;
        }
    }
}