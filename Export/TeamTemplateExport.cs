using D365.Community.Ps.Automation.Contract.Content;
using D365.Community.Ps.Automation.Service;
using System;
using System.Collections.Generic;
using System.IO;
using D365.Community.Ps.Automation.Data;

namespace D365.Community.Ps.Automation.Export
{
    internal static class TeamTemplateExport
    {
        internal static bool Execute(string directory, string prefix, string filter)
        {
            if (!DynamicsHelper.Etc2Etn(out var map)) return false;

            var queryFilter = string.IsNullOrWhiteSpace(filter) ? "" : $"&$filter={Uri.EscapeDataString(filter)}";
            var teamtemplates = $"{PsAutomation.ApiUrl}/teamtemplates?$select=teamtemplatename,teamtemplateid,objecttypecode,description,defaultaccessrightsmask{queryFilter}&$orderby={Uri.EscapeDataString("teamtemplatename asc")}";
            var result = Client.Fetch<ODataContents<List<TeamTemplate>>>(teamtemplates, out var response);
            if (response.StatusCode != 200) return result;
            var content = (ODataContents<List<TeamTemplate>>)response.Content;
            if (content.Value == null || content.Value.Count < 1) return true;
            //var templates = content.Value;

            var templates = new Contract.Config.TeamTemplates
            {
                CallerObjectId = PsAutomation.CallerObjectId,
                MsCrmCallerId = PsAutomation.MsCrmCallerId,
                Filter = filter
            };

            foreach (var template in content.Value)
            {
                templates.Templates.Add(new Contract.Config.TeamTemplate
                {
                    TeamTemplateName = template.TeamTemplateName,
                    TeamTemplateId = template.TeamTemplateId,
                    Description = template.Description,
                    DefaultAccessRightsMask = template.DefaultAccessRightsMask,
                    Entity = map[template.ObjectTypeCode]
                });
            }

            File.WriteAllText(Path.Combine(directory, $"{prefix}team-template.json"), Serializer.JsonSerialize(templates));

            return result;
        }


    }
}
