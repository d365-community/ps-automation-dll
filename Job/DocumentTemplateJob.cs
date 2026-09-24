using D365.Community.Ps.Automation.Contract.Config;
using D365.Community.Ps.Automation.Contract.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using D365.Community.Ps.Automation.Service;
using System.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Xml;
using D365.Community.Ps.Automation.Data;

namespace D365.Community.Ps.Automation.Job
{
    internal static class DocumentTemplateJob
    {
        internal static bool Execute(string directory, DocumentTemplates templates)
        {
            if (!DynamicsHelper.Etn2Etc(out var map)) return false;

            var documenttemplates = $"{PsAutomation.ApiUrl}/documenttemplates?$select=documenttemplateid,name,description,associatedentitytypecode,documenttype,languagecode,clientdata,content,status&$orderby={Uri.EscapeDataString("name asc")}";
            var result = Client.Fetch<ODataContents<List<Contract.Content.DocumentTemplate>>>(documenttemplates, out var response);
            if (response.StatusCode != 200) return result;

            var content = (ODataContents<List<Contract.Content.DocumentTemplate>>)response.Content;
            if (content.Value == null || content.Value.Count < 1) return true;
            var documentTemplates = content.Value;

            //find document template which need to be disabled (missing)
            Console.WriteLine("INFO: disable check");
            foreach (var disableTemplate in documentTemplates.Where(e => templates.Templates.All(c => !string.Equals($"{e.Name}({e.DocumentType})", $"{c.Name}({c.DocumentType})", StringComparison.InvariantCultureIgnoreCase))))
            {
                if (!templates.IgnoreMissing)
                {
                    //disable document template
                    Console.WriteLine($"INFO: disable document template: {disableTemplate.Name}");
                    var templateBody = new StringBuilder()
                        .OpenJson()
                        .AppendValue("status", 1)//Draft
                        .CloseJson();
                    result = Client.Patch($"{PsAutomation.ApiUrl}/documenttemplates({disableTemplate.DocumentTemplateId:D})", templateBody.ToString()) && result;
                }
                else
                {
                    Console.WriteLine($"INFO: skip not listed document template: {disableTemplate.Name}");
                }
            }

            //find queues need to be updated
            //update
            Console.WriteLine("INFO: update check");
            foreach (var updateTemplate in documentTemplates.Where(e => templates.Templates.Any(c => string.Equals($"{e.Name}({e.DocumentType})", $"{c.Name}({c.DocumentType})", StringComparison.InvariantCultureIgnoreCase))))
            {
                var localTemplate = templates.Templates.Single(e => string.Equals($"{e.Name}({e.DocumentType})", $"{updateTemplate.Name}({updateTemplate.DocumentType})", StringComparison.InvariantCultureIgnoreCase));

                if (localTemplate.ForceUpdate)
                {
                    Console.WriteLine($"INFO: update document template: {updateTemplate.Name}");
                    //delete old
                    result = Client.Delete($"{PsAutomation.ApiUrl}/documenttemplates({updateTemplate.DocumentTemplateId:D})") && result;

                    var templateBody = new StringBuilder()
                        .OpenJson()
                        .AppendValue("documenttemplateid", localTemplate.DocumentTemplateId ?? updateTemplate.DocumentTemplateId)
                        .AppendValue("name", localTemplate.Name.Trim().Replace(".xlsx", "").Replace(".docx", ""))
                        .AppendValue("documenttype", localTemplate.DocumentType)
                        .AppendValue("description", localTemplate.Description ?? updateTemplate.Description)
                        .AppendValue("languagecode", localTemplate.LanguageCode)
                        .AppendValue("content", GetContent(directory, templates.FileDir, localTemplate.File, localTemplate.DocumentType, map))
                        .CloseJson();
                    //create
                    result = Client.Post($"{PsAutomation.ApiUrl}/documenttemplates", templateBody.ToString(), out var id) && result;
                    updateTemplate.DocumentTemplateId = id;
                }
                if (updateTemplate.Status != localTemplate.Status)//disable
                {
                    //disable document template
                    Console.WriteLine($"INFO: disable document template: {updateTemplate.Name}");
                    var templateBody = new StringBuilder()
                        .OpenJson()
                        .AppendValue("status", 1)//Draft
                        .CloseJson();
                    result = Client.Patch($"{PsAutomation.ApiUrl}/documenttemplates({updateTemplate.DocumentTemplateId:D})", templateBody.ToString()) && result;
                }
            }

            return result;
        }

        private static string GetContent(string directory, string fileDir, string templateFile, int type, Dictionary<string, int> etcMap)
        {
            var fileName = Path.GetFileName(templateFile);
            var file = Path.Combine(directory, fileDir, Path.GetDirectoryName(templateFile) ?? "", fileName);
            if (!File.Exists(file))
            {
                file = Path.Combine(directory, fileDir, fileName);
                if (!File.Exists(file))
                {
                    file = Path.Combine(directory, file);
                    if (!File.Exists(file))
                    {
                        throw new FileNotFoundException($"Can't find file {file}...", file);
                    }
                }
            }

            //MicrosoftWord
            if (2 == type)
            {
                Console.WriteLine($"INFO: Read file {file}");
                var tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                Directory.CreateDirectory(tempDirectory);
                //extract docx to temp dir
                ZipFile.ExtractToDirectory(file, tempDirectory);
                foreach (var xmlFile in Directory.EnumerateFiles($"{tempDirectory}\\word", "*.xml", SearchOption.TopDirectoryOnly).Where(f => Regex.IsMatch(Path.GetFileNameWithoutExtension(f), "document|footer\\d{0,1}|header\\d{0,1}")))
                {
                    Console.WriteLine($"INFO: Read xml {xmlFile}");
                    //read word/document.xml
                    var document = new XmlDocument();
                    document.Load(xmlFile);
                    foreach (var entry in etcMap)
                    {
                        var matcher = $"({entry.Key}/" + @"\d{1,5})";
                        var replacement = $"{entry.Key}/{entry.Value}";
                        if (document.DocumentElement != null) document.DocumentElement.InnerXml = ReplacePatternInXml(document.DocumentElement.InnerXml, matcher, replacement);
                    }
                    Console.WriteLine($"INFO: Write xml {file}");
                    document.Save(xmlFile);
                }
                //delete zip before creating a new file instead
                File.Delete(file);
                ZipFile.CreateFromDirectory(tempDirectory, file);
            }
            //read
            Console.WriteLine($"INFO: Load file {file}");
            return Convert.ToBase64String(File.ReadAllBytes(file));
        }

        private static string ReplacePatternInXml(string xml, string matcher, string replacement)
        {
            var match = Regex.Match(xml, matcher, RegexOptions.CultureInvariant | RegexOptions.Multiline);
            while (match.Success)
            {
                foreach (Group group in match.Groups)
                {
                    foreach (Capture capture in group.Captures)
                    {
                        if (capture.Value != replacement)
                        {
                            Console.WriteLine($"INFO: Replace etc: {capture} -> {replacement}");
                            xml = xml.Replace(capture.Value, replacement);
                        }
                    }
                }
                match = match.NextMatch();
            }
            return xml;
        }
    }
}
