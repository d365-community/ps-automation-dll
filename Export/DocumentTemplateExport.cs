using System;
using System.Collections.Generic;
using System.IO;
using D365.Community.Ps.Automation.Contract.Content;
using D365.Community.Ps.Automation.Service;

namespace D365.Community.Ps.Automation.Export
{
    internal static class DocumentTemplateExport
    {
        internal static bool Execute(string directory, string prefix, string filter)
        {
            var queryFilter = string.IsNullOrWhiteSpace(filter) ? "" : $"&$filter={Uri.EscapeDataString(filter)}";
            var documenttemplates = $"{PsAutomation.ApiUrl}/documenttemplates?$select=documenttemplateid,name,description,associatedentitytypecode,documenttype,languagecode,clientdata,content,status{queryFilter}&$orderby={Uri.EscapeDataString("name asc")}";
            var result = Client.Fetch<ODataContents<List<DocumentTemplate>>>(documenttemplates, out var response);
            if (response.StatusCode != 200) return result;
            var content = (ODataContents<List<DocumentTemplate>>)response.Content;
            if (content.Value == null || content.Value.Count < 1) return true;
            //var templates = content.Value;

            var templates = new Contract.Config.DocumentTemplates
            {
                CallerObjectId = PsAutomation.CallerObjectId,
                MsCrmCallerId = PsAutomation.MsCrmCallerId,
                Filter = filter
            };
            foreach (var template in content.Value)
            {
                var templateName = $"{template.Name}{(template.DocumentType == 1 ? ".xlsx" : ".docx")}";

                templates.Templates.Add(new Contract.Config.DocumentTemplate
                {
                    Name = template.Name,
                    AssociatedEntityTypeCode = template.AssociatedEntityTypeCode,
                    Description = template.Description,
                    DocumentTemplateId = template.DocumentTemplateId,
                    DocumentType = template.DocumentType,
                    LanguageCode = template.LanguageCode,
                    Status = template.Status,
                    ForceUpdate = false,
                    File = $".\\{prefix}{templateName}"
                });

                var fileName = Path.Combine(directory, $"{prefix}{templateName}");

                if (File.Exists(fileName))
                {
                    try
                    {
                        File.Delete(fileName);
                    }
                    catch (Exception e)
                    {
                        Console.Error.WriteLine($"ERROR: {e.GetBaseException().Message}");
                        result = false;
                    }
                }
                try
                {
                    Console.WriteLine($"INFO: File:{fileName}");
                    File.WriteAllBytes(fileName, Convert.FromBase64String(template.Content));
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine($"ERROR: {e.GetBaseException().Message}");
                    result = false;
                }
            }
            File.WriteAllText(Path.Combine(directory, $"{prefix}document-template.json"), Serializer.JsonSerialize(templates));

            return result;
        }
    }
}
