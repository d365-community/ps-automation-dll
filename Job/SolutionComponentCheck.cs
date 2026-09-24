using System;
using System.Collections.Generic;
using System.Linq;
using D365.Community.Ps.Automation.Contract.Config;
using D365.Community.Ps.Automation.Contract.Content;

namespace D365.Community.Ps.Automation.Job
{
    /// <summary>
    /// Check that only approved components are in the solution.
    /// </summary>
    internal static class SolutionComponentCheck
    {
        /// <summary>
        /// get entities (metadata)<br/><br/>
        /// see https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/reference/retrieveallentities?view=dataverse-latest
        /// </summary>
        /// <returns>false, if any index creation failed, otherwise true</returns>
        internal static bool Execute(SolutionsComponents config)
        {
            var result = true;
            foreach (var solutionComponents in config.ListSolutionComponents)
            {
                Console.WriteLine($"INFO: solution '{solutionComponents.Solution}' (incl. all patches)");
                result = GetSolutions(solutionComponents.Solution, out var solutions) && result;
                if (!result) break;
                foreach (var solution in solutions)
                {
                    Console.WriteLine($"INFO: -> '{solution.Value}' ({solution.Key:D})");
                }
                var action = solutionComponents.Action == SolutionComponentsAction.Exclude ? "In" : "NotIn";
                var filterSolutions = string.Join(",", solutions.Keys.Select(i => $"'{i:D}'"));
                var filterTypes = string.Join(",", solutionComponents.Types.Select(i => $"'{i}'"));
                var components = $"{PsAutomation.ApiUrl}/solutioncomponents?$select=componenttype,objectid,_solutionid_value&$filter=rootsolutioncomponentid{Uri.EscapeDataString(" eq null and ")}Microsoft.Dynamics.CRM.{action}(PropertyName=@p1,PropertyValues=@p2){Uri.EscapeDataString(" and ")}Microsoft.Dynamics.CRM.In(PropertyName=@p3,PropertyValues=@p4)&@p1='componenttype'&@p2=[{filterTypes}]&@p3='solutionid'&@p4=[{filterSolutions}]&$orderby={Uri.EscapeDataString("_solutionid_value asc")}";
                result = Client.Fetch<ODataContents<List<SolutionComponent>>>(components, out var response) && result;
                if (response.StatusCode == 200)
                {
                    var content = (ODataContents<List<SolutionComponent>>)response.Content;
                    if (content.Value == null || content.Value.Count < 1) continue;
                    foreach (var component in content.Value)
                    {
                        if (solutionComponents.Strict)
                        {
                            result = false;
                            Console.Error.WriteLine($"ERROR:   {solutions[component.SolutionId]} contains component type {component.ComponentType}");
                        }
                        else
                        {
                            Console.WriteLine($"WARNING: {solutions[component.SolutionId]} contains component type {component.ComponentType}");
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
