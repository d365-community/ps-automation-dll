using D365.Community.Ps.Automation.Contract.Content;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using D365.Community.Ps.Automation.Service;
using D365.Community.Ps.Automation.Contract.Config;

namespace D365.Community.Ps.Automation.Job
{
    internal static class WebResourceRegistrationJob
    {
        internal static bool Execute(string directory, WebResourceRegistration registrations)
        {
            var result = true;

            var targets = new Dictionary<string, string>();

            List<string> searchPattern;
            if (registrations.WebResourcePatterns?.Any() ?? false)
            {
                searchPattern = registrations.WebResourcePatterns;
            }
            else
            {
                searchPattern = new List<string> { "*.*" };
            }

            var searchOption = SearchOption.TopDirectoryOnly;
            if (registrations.WebResourceRecursive)
            {
                searchOption = SearchOption.AllDirectories;
            }

            var webResourceFolder = Path.GetFullPath(Path.Combine(directory.TrimEnd('\\'), registrations.WebResourceFolder));

            var files = GetFiles(webResourceFolder, searchPattern, searchOption);
            foreach (var file in files)
            {
                if (registrations.WebResourceMap?.Any() ?? false)
                {
                    if (registrations.WebResourceMap.TryGetValue(GetRelativePath(file, webResourceFolder), out var path))
                    {
                        targets[path] = file;
                    }
                    else
                    {
                        Console.WriteLine($"INFO: skip - file '{file}' is not mapped");
                    }
                }
                else
                {
                    var prefix = registrations.WebResourcePrefix?.Trim() ?? string.Empty;

                    if (registrations.WebResourceKeepFolderStructure)
                    {
                        targets[$"{prefix}/{GetRelativePath(file, webResourceFolder)}"] = file;
                    }
                    else
                    {
                        targets[$"{prefix}/{Path.GetFileName(file)}"] = file;
                    }
                }
            }

            foreach (var target in targets)
            {
                var type = GetWebResourceType(Path.GetExtension(target.Value));
                var webresourceset = $"{PsAutomation.ApiUrl}/webresourceset?$select=name,content,languagecode&$filter={Uri.EscapeDataString($"name eq '{target.Key}' and webresourcetype eq {type}")}&$orderby={Uri.EscapeDataString("name asc")}";
                result = Client.Fetch<ODataContents<List<WebResource>>>(webresourceset, out var response) && result;
                if (response.StatusCode != 200 || type == null) continue;

                var body = GetBase64String(target.Value);
                var languagecode = 0;
                var fileName = Path.GetFileName(target.Value);
                var extension = "\\" + Path.GetExtension(target.Value);
                if (Regex.IsMatch(fileName, "^.*\\.\\d{4}" + extension + "$"))
                {
                    languagecode = int.Parse(Regex.Match(fileName, "^.*\\.(\\d{4})" + extension + "$").Groups[1].Value);
                }

                var content = (ODataContents<List<WebResource>>)response.Content;
                if (content.Value == null || content.Value.Count < 1)
                {
                    //create
                    var webResourceBody = new StringBuilder()
                        .OpenJson()
                        .AppendValue("name", target.Key)
                        .AppendValue("displayname", target.Key)
                        .AppendValue("description", $"{DateTime.UtcNow:s}(UTC): Created WebResource")
                        .AppendValue("content", body)
                        .AppendValue("webresourcetype", type.Value);
                    if (languagecode != 0)
                    {
                        webResourceBody.AppendValue("languagecode", languagecode);
                    }
                    webResourceBody.CloseJson();
                    Console.WriteLine($"INFO: create webresource '{target.Key}'");
                    result = Client.Post($"{PsAutomation.ApiUrl}/webresourceset", webResourceBody.ToString(), out var webResourceId) && result;
                    if (registrations.WebResourcePublish)
                    {
                        result = PublishWebResource(webResourceId, target.Key) && result;
                    }
                    if (!string.IsNullOrWhiteSpace(registrations.WebResourceSolution))
                    {
                        var addSolutionComponentBody = new StringBuilder()
                            .OpenJson()
                            .AppendValue("ComponentId", webResourceId)
                            .AppendValue("ComponentType", 61)
                            .AppendValue("SolutionUniqueName", registrations.WebResourceSolution)
                            .AppendValue("AddRequiredComponents", false)
                            .CloseJson();
                        Console.WriteLine($"INFO: add webresource '{target.Key}' to solution '{registrations.WebResourceSolution}'");
                        result = Client.Post<AddSolutionComponent>($"{PsAutomation.ApiUrl}/AddSolutionComponent()", addSolutionComponentBody.ToString(), out _) && result;
                    }
                }
                else if (content.Value.Count > 1)
                {
                    Console.Error.WriteLine($"ERROR: {target.Key}({type.Value}) not unique!");
                    result = false;
                    continue;
                }
                else
                {
                    var webresource = content.Value[0];
                    //update
                    if (!string.Equals(webresource.Content, body, StringComparison.InvariantCulture) || (webresource.LanguageCode ?? 0) != languagecode)
                    {
                        var webResourceBody = new StringBuilder()
                            .OpenJson()
                            .AppendValue("displayname", target.Key)
                            .AppendValue("description", $"{DateTime.UtcNow:s}(UTC): Updated WebResource")
                            .AppendValue("content", body);
                        if (languagecode != 0)
                        {
                            webResourceBody.AppendValue("languagecode", languagecode);
                        }
                        else
                        {
                            webResourceBody.AppendNull("languagecode");
                        }
                        webResourceBody.CloseJson();
                        Console.WriteLine($"INFO: update webresource '{target.Key}'");
                        result = Client.Patch($"{PsAutomation.ApiUrl}/webresourceset({webresource.WebResourceId:D})", webResourceBody.ToString()) && result;
                        if (registrations.WebResourcePublish)
                        {
                            result = PublishWebResource(webresource.WebResourceId, target.Key) && result;
                        }
                    }
                    else
                    {
                        Console.WriteLine($"INFO: skip - webresource '{target.Key}' is up to date");
                    }
                }
            }

            return result;
        }

        private static bool PublishWebResource(Guid webResourceId, string name)
        {
            var publishXmlBody = new StringBuilder()
                .OpenJson()
                .AppendValue("ParameterXml", $"<importexportxml><webresources><webresource>{webResourceId:B}</webresource></webresources></importexportxml>")
                .CloseJson();
            Console.WriteLine($"INFO: publish webresource '{name}'");
            return Client.Post($"{PsAutomation.ApiUrl}/PublishXml()", publishXmlBody.ToString(), out _);
        }

        private static List<string> GetFiles(string sourceFolder, List<string> filters, SearchOption searchOption)
        {
            var files = new List<string>();

            foreach (var filter in filters)
            {
                files.AddRange(Directory.GetFiles(sourceFolder, filter, searchOption));
            }

            return files;
        }

        private static string GetRelativePath(string fullPath, string basePath)
        {
            if (!basePath.EndsWith("\\"))
            {
                basePath += "\\";
            }
            return new Uri(basePath).MakeRelativeUri(new Uri(fullPath)).ToString();
        }

        private static int? GetWebResourceType(string extension)
        {
            extension = extension.TrimStart('.').ToLowerInvariant();

            switch (extension)
            {
                case "png":
                    return 5;
                case "svg":
                    return 11;
                case "gif":
                    return 7;
                case "ico":
                    return 10;
                case "jpeg":
                    return 6;
                case "html":
                    return 1;
                case "css":
                    return 2;
                case "xsl":
                    return 9;
                case "resx":
                    return 12;
                case "js":
                    return 3;
                case "xml":
                    return 4;
                default:
                    Console.Error.WriteLine($"Unknown File Extension detected: {extension}");
                    return null;
            }
        }

        private static string GetBase64String(string file)
        {
            using (var fs = new FileStream(file, FileMode.Open, FileAccess.Read))
            {
                var binaryData = new byte[fs.Length];
                _ = fs.Read(binaryData, 0, (int)fs.Length);
                fs.Close();
                return Convert.ToBase64String(binaryData, 0, binaryData.Length);
            }
        }
    }
}
