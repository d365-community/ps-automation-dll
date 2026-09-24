using D365.Community.Ps.Automation.Contract.Config;
using D365.Community.Ps.Automation.Contract.Content;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Text;
using System.IO;

namespace D365.Community.Ps.Automation.Job
{
    internal static class ComponentLayersReview
    {
        internal static bool Execute(string directory, string prefix, ComponentLayers config)
        {
            var result = true;
            var items = new SortedDictionary<string, HashSet<Guid>>();
            var review = new StringBuilder();

            foreach (var layer in config.Layers)
            {
                Console.WriteLine($"INFO: solution '{layer.Solution}' (incl. all patches)");
                result = GetSolutions(layer.Solution, out var solutions) && result;
                if (!result) break;
                foreach (var solution in solutions)
                {
                    Console.WriteLine($"INFO: -> '{solution.Value}' ({solution.Key:D})");
                }
                var filter = string.Join(",", solutions.Keys.Select(i => $"'{i:D}'"));

                foreach (var type in layer.Types)
                {
                    if (!items.ContainsKey(type)) items[type] = new HashSet<Guid>();
                    int componenttype;
                    switch (type)
                    {
                        case "Entity":
                            componenttype = 1;
                            break;
                        case "Role":
                            componenttype = 20;
                            break;
                        case "AppModule":
                            componenttype = 80;
                            break;
                        case "WebResource":
                            componenttype = 61;
                            break;
                        case "SdkMessageProcessingStep":
                            componenttype = 92;
                            break;
                        case "Workflow":
                            componenttype = 29;
                            break;
                        case "PluginAssembly":
                            componenttype = 91;
                            break;
                        default:
                            Console.WriteLine("WARNING: Only \"Entity\", \"Role\", \"AppModule\", \"WebResource\", \"SdkMessageProcessingStep\", \"Workflow\", \"PluginAssembly\" is implemented!");
                            continue;
                    }

                    var components = $"{PsAutomation.ApiUrl}/solutioncomponents?$select=componenttype,objectid,_solutionid_value&$filter={Uri.EscapeDataString($"componenttype eq {componenttype} and ")}Microsoft.Dynamics.CRM.In(PropertyName=@p1,PropertyValues=@p2)&@p1='solutionid'&@p2=[{filter}]&$orderby={Uri.EscapeDataString("_solutionid_value asc,objectid asc")}";
                    result = Client.Fetch<ODataContents<List<SolutionComponent>>>(components, out var response) && result;
                    if (response.StatusCode == 200)
                    {
                        var content = (ODataContents<List<SolutionComponent>>)response.Content;
                        if (content.Value == null || content.Value.Count < 1) continue;
                        foreach (var component in content.Value)
                        {
                            items[type].Add(component.ObjectId);
                        }
                    }
                }
            }

            review.AppendLine($"{"Component",-30} | Order | {"Solution",-50} | {"Name",-180}");

            foreach (var item in items)
            {
                review.AppendLine($"{new string('-', 31)}+{new string('-', 7)}+{new string('-', 52)}+{new string('-', 181)}");

                foreach (var objectid in item.Value)
                {
                    var componentlayers = $"{PsAutomation.ApiUrl}/msdyn_componentlayers?$select=msdyn_name,msdyn_solutioncomponentname,msdyn_solutionname,msdyn_order&$filter={Uri.EscapeDataString($"msdyn_componentid eq '{objectid}' and msdyn_solutioncomponentname eq '{item.Key}'")}&$orderby={Uri.EscapeDataString("msdyn_order desc")}";
                    result = Client.Fetch<ODataContents<List<Contract.Content.ComponentLayer>>>(componentlayers, out var response) && result;
                    var content = (ODataContents<List<Contract.Content.ComponentLayer>>)response.Content;
                    if (content.Value == null || content.Value.Count < 1) continue;
                    foreach (var componentlayer in content.Value)
                    {
                        var name = componentlayer.Name;
                        if (componentlayer.SolutionComponentName == "Workflow")
                        {
                            if (GetWorkflow(objectid, out Workflow workflow))
                            {
                                switch (workflow.Category)
                                {
                                    case 0: //Workflow
                                    case 1: //Dialog
                                    case 5: //ModernFlow
                                    case 6: //DesktopFlow
                                        name = workflow.Name;
                                        break;
                                    case 2: //BusinessRule
                                        name = $"{workflow.Name}.[{workflow.PrimaryEntity}]";
                                        break;
                                    case 3: //Action
                                    case 4: //BusinessProcessFlow
                                        name = workflow.UniqueName;
                                        break;
                                    default:
                                        continue;
                                }
                            }
                        }
                        review.AppendLine($"{componentlayer.SolutionComponentName,-30} | {componentlayer.Order,5} | {componentlayer.SolutionName,-50} | {name,-180}");
                    }
                }
            }

            Console.WriteLine(review.ToString());
            File.WriteAllText(Path.Combine(directory, $"{prefix}component-layer-review.log"), review.ToString());

            return result;
        }

        private static bool GetWorkflow(Guid id, out Workflow entity)
        {
            entity = null;
            var workflow = $"{PsAutomation.ApiUrl}/workflows({id})?$select=workflowid,category,name,uniquename,primaryentity";
            var result = Client.Get<Workflow>(workflow, out var response);
            if (response.StatusCode == 200)
            {
                entity = (Workflow)response.Content;
            }
            return result;
        }

        private static bool GetSolutions(string uniquename, out Dictionary<Guid, string> solutionIds)
        {
            solutionIds = new Dictionary<Guid, string>();
            var solutions = $"{PsAutomation.ApiUrl}/solutions?$select=solutionid,uniquename&$filter={Uri.EscapeDataString($"uniquename eq '{uniquename}' or startswith(uniquename,'{uniquename}_Patch_')")}";
            var result = Client.Fetch<ODataContents<List<Solution>>>(solutions, out var response);
            if (response.StatusCode == 200)
            {
                var content = (ODataContents<List<Solution>>)response.Content;
                if (content.Value == null || content.Value.Count < 1)
                {
                    Console.Error.WriteLine($"ERROR: solution '{uniquename}' not found!");
                    return false;//not found
                }

                foreach (var solution in content.Value)
                {
                    solutionIds.Add(solution.SolutionId, solution.UniqueName);
                }
            }
            return result;
        }
    }
}
