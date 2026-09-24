using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using D365.Community.Ps.Automation.Contract.Config;
using D365.Community.Ps.Automation.Contract.Content;

namespace D365.Community.Ps.Automation.Job
{
    /// <summary>
    /// Check that only approved entities come with all assets.
    /// </summary>
    internal static class EntityAllAssetsCheck
    {
        /// <summary>
        /// get entities (metadata)<br/><br/>
        /// see https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/reference/retrieveallentities?view=dataverse-latest
        /// </summary>
        /// <returns>false, if any index creation failed, otherwise true</returns>
        internal static bool Execute(EntitiesAllAssets config)
        {
            var retrieveAllEntities = $"{PsAutomation.ApiUrl}/RetrieveAllEntities(EntityFilters='Entity',RetrieveAsIfPublished=true)";
            var result = Client.Get<AllEntities>(retrieveAllEntities, out var response);
            if (response.StatusCode == 200)
            {
                var content = (AllEntities)response.Content;
                if (content.EntityMetadata == null || content.EntityMetadata.Count < 1) return false;
                var entities = content.EntityMetadata;

                foreach (var entityAllAssets in config.ListOfEntityAllAssets)
                {
                    Console.WriteLine($"INFO: solution '{entityAllAssets.Solution}' (incl. all patches)");
                    result = GetSolutions(entityAllAssets.Solution, out var solutions) && result;
                    if (!result) break;
                    foreach (var solution in solutions)
                    {
                        Console.WriteLine($"INFO: -> '{solution.Value}' ({solution.Key:D})");
                    }
                    var filter = string.Join(",", solutions.Keys.Select(i => $"'{i:D}'"));
                    var components = $"{PsAutomation.ApiUrl}/solutioncomponents?$select=componenttype,objectid,_solutionid_value,rootcomponentbehavior,ismetadata&$filter={Uri.EscapeDataString("componenttype eq 1 and ")}Microsoft.Dynamics.CRM.In(PropertyName=@p1,PropertyValues=@p2)&@p1='solutionid'&@p2=[{filter}]&$orderby={Uri.EscapeDataString("_solutionid_value asc,objectid asc")}";
                    result = Client.Fetch<ODataContents<List<SolutionComponent>>>(components, out var innerresponse) && result;
                    if (innerresponse.StatusCode == 200)
                    {
                        var innercontent = (ODataContents<List<SolutionComponent>>)innerresponse.Content;
                        if (innercontent.Value == null || innercontent.Value.Count < 1) continue;
                        foreach (var component in innercontent.Value)
                        {
                            var entity = entities.Single(e => e.MetadataId == component.ObjectId);
                            Console.WriteLine($"INFO:   entity '{entity.LogicalName}' ({entity.DisplayName.UserLocalizedLabel?.Label}):");

                            //check whitelist
                            if (component.RootComponentBehavior == 0)//Include Subcomponents
                            {
                                Console.WriteLine("INFO:    -> Include Subcomponents (All Assets)");
                                var approved = false;
                                foreach (var entryWl in entityAllAssets.WhiteList)
                                {
                                    var patternWl = entryWl;
                                    var invertWl = false;
                                    if (entryWl.StartsWith("!"))
                                    {
                                        patternWl = entryWl.Remove(0, 1);
                                        invertWl = true;
                                    }
                                    if (Regex.IsMatch(entity.LogicalName, patternWl))
                                    {
                                        approved = !invertWl;
                                        foreach (var entryBl in entityAllAssets.BlackList)
                                        {
                                            var patternBl = entryBl;
                                            var invertBl = false;
                                            if (entryBl.StartsWith("!"))
                                            {
                                                patternBl = entryBl.Remove(0, 1);
                                                invertBl = true;
                                            }
                                            if (Regex.IsMatch(entity.LogicalName, patternBl))
                                            {
                                                approved = invertBl;
                                                break;
                                            }
                                        }
                                        break;
                                    }
                                }
                                if (approved)
                                {
                                    Console.WriteLine("INFO:    -> approved: true");
                                }
                                else
                                {
                                    if (entityAllAssets.Strict)
                                    {
                                        result = false;
                                        Console.Error.WriteLine($"ERROR:   -> approved: false (see {solutions[component.SolutionId]})");
                                    }
                                    else
                                    {
                                        Console.WriteLine("WARNING: -> approved: false");
                                    }
                                }
                            }
                            else if (component.RootComponentBehavior == 1)//Do not include subcomponents
                            {
                                Console.WriteLine("INFO:    -> Do not include subcomponents");
                            }
                            else if (component.RootComponentBehavior == 2)//Include As Shell Only
                            {
                                Console.WriteLine("INFO:    -> Include As Shell Only");
                            }
                            else
                            {
                                Console.WriteLine($"INFO:    -> RootComponentBehavior {component.RootComponentBehavior}");
                            }
                        }
                    }
                }
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
