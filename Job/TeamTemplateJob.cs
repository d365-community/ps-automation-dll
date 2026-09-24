using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using D365.Community.Ps.Automation.Contract.Content;
using D365.Community.Ps.Automation.Data;
using D365.Community.Ps.Automation.Service;

namespace D365.Community.Ps.Automation.Job
{
    internal static class TeamTemplateJob
    {
        internal static bool Execute(Contract.Config.TeamTemplates config)
        {
            if (!DynamicsHelper.Etn2Etc(out var map)) return false;

            var teamtemplates = $"{PsAutomation.ApiUrl}/teamtemplates?$select=teamtemplatename,teamtemplateid,objecttypecode,description,defaultaccessrightsmask&$orderby={Uri.EscapeDataString("teamtemplatename asc")}";
            var result = Client.Fetch<ODataContents<List<Contract.Content.TeamTemplate>>>(teamtemplates, out var response);
            if (response.StatusCode != 200) return result;
            var content = (ODataContents<List<Contract.Content.TeamTemplate>>)response.Content;
            if (content.Value == null || content.Value.Count < 1) return true;
            var templates = content.Value;

            //find templates which need to be deleted
            Console.WriteLine("INFO: delete check");
            foreach (var template in config.Templates.Where(e => templates.All(c => e.TeamTemplateId != c.TeamTemplateId)))
            {
                //delete template
                Console.WriteLine($"INFO: delete template: {template.TeamTemplateName}");
                result = Client.Delete($"{PsAutomation.ApiUrl}/teamtemplates({template.TeamTemplateId:D})") && result;
            }

            //TODO: improve later on if needed
            //var retrieveAllEntities = $"{PsAutomation.ApiUrl}/RetrieveAllEntities(EntityFilters='Entity',RetrieveAsIfPublished=true)";
            //var result = Client.Get<AllEntities>(retrieveAllEntities, out var response);

            //find templates which need to be updated
            Console.WriteLine("INFO: update check");
            foreach (var template in config.Templates.Where(c => templates.Any(e => e.TeamTemplateId == c.TeamTemplateId)))
            {
                var existing = templates.Single(e => e.TeamTemplateId == template.TeamTemplateId);

                var update = false;
                var templateBody = new StringBuilder().OpenJson();

                if (!string.Equals(template.TeamTemplateName, existing.TeamTemplateName, StringComparison.InvariantCultureIgnoreCase))
                {
                    update = true;
                    templateBody.AppendValue("teamtemplatename", template.TeamTemplateName);
                }

                if (!string.Equals(template.Description, existing.Description, StringComparison.InvariantCultureIgnoreCase))
                {
                    update = true;
                    templateBody.AppendValue("description", template.Description);
                }

                if (map[template.Entity] != existing.ObjectTypeCode)
                {
                    update = true;
                    templateBody.AppendValue("objecttypecode", map[template.Entity]);
                }

                if (template.DefaultAccessRightsMask != existing.DefaultAccessRightsMask)
                {
                    update = true;
                    templateBody.AppendValue("defaultaccessrightsmask", template.DefaultAccessRightsMask);
                }

                templateBody.CloseJson();

                if (update)
                {
                    result = Client.Patch($"{PsAutomation.ApiUrl}/teamtemplates({template.TeamTemplateId:D})", templateBody.ToString()) && result;
                }
            }

            //find templates which need to be created
            Console.WriteLine("INFO: create check");
            foreach (var template in config.Templates.Where(c => templates.All(e => e.TeamTemplateId != c.TeamTemplateId)))
            {
                var templateBody = new StringBuilder()
                    .OpenJson()
                    .AppendValue("teamtemplateid", template.TeamTemplateId)
                    .AppendValue("teamtemplatename", template.TeamTemplateName)
                    .AppendValue("description", template.Description)
                    .AppendValue("objecttypecode", map[template.Entity])
                    .AppendValue("defaultaccessrightsmask", template.DefaultAccessRightsMask)
                    .CloseJson();
                result = Client.Post($"{PsAutomation.ApiUrl}/teamtemplates", templateBody.ToString(), out _) && result;
            }
            return result;
        }
    }
}
