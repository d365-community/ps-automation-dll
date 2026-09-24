using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;
using D365.Community.Ps.Automation.Contract.Config;
using D365.Community.Ps.Automation.Contract.Content;
using D365.Community.Ps.Automation.Service;

namespace D365.Community.Ps.Automation.Job
{
    internal static class ProcessOwnerJob
    {
        internal static bool Execute(ProcessOwner config, string filter)
        {
            var psDebug = bool.Parse(Environment.GetEnvironmentVariable("D365_PS_DEBUG") ?? "false");

            if (config.AllowAllOwner) Console.WriteLine("WARNING: allow all owner is enabled, this could cause problems!");
            var result = GetSystemUsers(out var systemusers);
            result = Get_SYSTEM_UserId(out var systemUserId) && result;
            var queryFilter = string.IsNullOrWhiteSpace(filter) ? "" : Uri.EscapeDataString($" and ({filter})");
            //componentstate:published and type:definition
            var workflows = $"{PsAutomation.ApiUrl}/workflows?$select=category,name,uniquename,statecode,statuscode,primaryentity,_ownerid_value,_owninguser_value,_owningteam_value&$filter={Uri.EscapeDataString("componentstate eq 0 and type eq 1")}{queryFilter}&$orderby={Uri.EscapeDataString("name asc")}";
            result = Client.Fetch<ODataContents<List<Workflow>>>(workflows, out var response) && result;
            if (response.StatusCode == 200)
            {
                var content = (ODataContents<List<Workflow>>)response.Content;
                if (content.Value == null || content.Value.Count < 1) return true;
                foreach (var workflow in content.Value)
                {
                    var owner = systemusers.Value.FirstOrDefault(s => s.SystemUserId == workflow.OwnerId);

                    if (owner == null)
                    {
                        Console.Error.WriteLine($"ERROR: unresolvable owner {workflow.OwnerId}");
                        result = false;
                        continue;
                    }
                    var fullName = owner.FullName;
                    var domainName = owner.DomainName ?? string.Empty;
                    var application = $"{(owner.ApplicationId == null ? "" : owner.ApplicationId.Value.ToString("D"))}";
                    var user = string.IsNullOrWhiteSpace(owner.DomainName) ? owner.FullName : owner.DomainName;
                    string domain;
                    try
                    {
                        domain = "@" + new MailAddress(owner.DomainName).Host; // host contains e.g. outlook.de
                    }
                    catch
                    {
                        // ignored, nothing to worry about
                        domain = "empty";
                    }

                    //potential skip
                    if ("SYSTEM".Equals(fullName, StringComparison.InvariantCulture) && !config.AllowAllOwner)
                    {
                        Console.WriteLine($"INFO: skip '{workflow.Name}' ({TranslateCategory(workflow.Category)}); owned by {fullName}");
                        continue;
                    }
                    if (domain.Equals("@onmicrosoft.com", StringComparison.InvariantCultureIgnoreCase) && !config.AllowAllOwner)
                    {
                        Console.WriteLine($"INFO: skip '{workflow.Name}' ({TranslateCategory(workflow.Category)}); owned by {domainName}");
                        continue;
                    }
                    if (config.WhitelistedDomainNames.Contains(domainName) || config.WhitelistedDomainNames.Contains(domain))
                    {
                        Console.WriteLine($"INFO: skip '{workflow.Name}' ({TranslateCategory(workflow.Category)}); owned by {owner.DomainName}");
                        continue;
                    }

                    //potential remap
                    foreach (var map in config.ApplicationMap)
                    {
                        var guid = map.Key.ToString("D");
                        //owner: App:8cc93351-ae3b-4d5d-836e-fda28cee7497; map: { 8cc93351-ae3b-4d5d-836e-fda28cee7497, admin@sample.onmicrosoft.com/369fa706-f638-4dcd-9482-03206de45fd5 } target could be an app or an user
                        if (application.Equals(guid, StringComparison.InvariantCultureIgnoreCase))
                        {
                            SystemUser assignee;
                            if (Guid.TryParse(map.Value, out var applicationId))
                            {
                                assignee = systemusers.Value.FirstOrDefault(s => s.ApplicationId == applicationId);
                                if (assignee == null)
                                {
                                    Console.Error.WriteLine($"ERROR: unresolvable application {map.Value}");
                                    result = false;
                                    continue;
                                }
                            }
                            else if (string.Equals("SYSTEM", map.Value, StringComparison.InvariantCultureIgnoreCase))
                            {
                                assignee = new SystemUser
                                {
                                    SystemUserId = systemUserId
                                };
                            }
                            else
                            {
                                assignee = systemusers.Value.FirstOrDefault(s => string.Equals(s.DomainName, map.Value, StringComparison.CurrentCultureIgnoreCase) || string.Equals(s.FullName, map.Value, StringComparison.CurrentCultureIgnoreCase));
                                if (assignee == null)
                                {
                                    Console.Error.WriteLine($"ERROR: unresolvable user {map.Value}");
                                    result = false;
                                    continue;
                                }
                            }
                            if (assignee.SystemUserId == owner.SystemUserId)
                            {
                                Console.WriteLine($"INFO: skip '{workflow.Name}' ({TranslateCategory(workflow.Category)}); owned by {owner.DomainName}");
                                continue;
                            }
                            if (config.Excluded != null)
                            {
                                var excluded = false;
                                switch (workflow.Category)
                                {
                                    case 0: //Workflow
                                        excluded = config.Excluded.Workflows.Contains(workflow.Name);
                                        break;
                                    case 1: //Dialog
                                        excluded = config.Excluded.Dialogs.Contains(workflow.Name);
                                        break;
                                    case 2: //BusinessRule
                                        excluded = config.Excluded.BusinessRules.Contains($"{workflow.Name}.[{workflow.PrimaryEntity}]");
                                        break;
                                    case 3: //Action
                                        excluded = config.Excluded.Actions.Contains(workflow.UniqueName);
                                        break;
                                    case 4: //BusinessProcessFlow
                                        excluded = config.Excluded.BusinessProcessFlows.Contains(workflow.UniqueName);
                                        break;
                                    case 5: //ModernFlow
                                        excluded = config.Excluded.ModernFlows.Contains(workflow.Name);
                                        break;
                                    case 6: //DesktopFlow
                                        excluded = config.Excluded.DesktopFlows.Contains(workflow.Name);
                                        break;
                                }
                                if (excluded)
                                {
                                    Console.WriteLine($"INFO: excluded '{workflow.Name}' ({TranslateCategory(workflow.Category)}); owned by {owner.DomainName}");
                                    continue;
                                }
                            }
                            result = UpdateOwner(workflow, assignee.SystemUserId) && result;
                            break;
                        }
                    }

                    foreach (var map in config.DomainOwnerMap)
                    {
                        //owner: User:user.test@outlook.de; map: { .*@outlook\\.[de|com|org], admin@sample.onmicrosoft.com/369fa706-f638-4dcd-9482-03206de45fd5 } target could be an app or an user
                        if (Regex.IsMatch(user, map.Key, RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant))
                        {
                            SystemUser assignee;
                            if (Guid.TryParse(map.Value, out var applicationId))
                            {
                                assignee = systemusers.Value.FirstOrDefault(s => s.ApplicationId == applicationId);
                                if (assignee == null)
                                {
                                    Console.Error.WriteLine($"ERROR: unresolvable application {map.Value}");
                                    result = false;
                                    continue;
                                }
                            }
                            else if (string.Equals("SYSTEM", map.Value, StringComparison.InvariantCultureIgnoreCase))
                            {
                                assignee = new SystemUser
                                {
                                    SystemUserId = systemUserId
                                };
                            }
                            else
                            {
                                assignee = systemusers.Value.FirstOrDefault(s => string.Equals(s.DomainName, map.Value, StringComparison.CurrentCultureIgnoreCase) || string.Equals(s.FullName, map.Value, StringComparison.CurrentCultureIgnoreCase));
                                if (assignee == null)
                                {
                                    Console.Error.WriteLine($"ERROR: unresolvable user {map.Value}");
                                    result = false;
                                    continue;
                                }
                            }
                            if (assignee.SystemUserId == owner.SystemUserId)
                            {
                                Console.WriteLine($"INFO: skip '{workflow.Name}' ({TranslateCategory(workflow.Category)}); owned by {owner.DomainName}");
                                continue;
                            }
                            if (config.Excluded != null)
                            {
                                var excluded = false;
                                switch (workflow.Category)
                                {
                                    case 0: //Workflow
                                        excluded = config.Excluded.Workflows.Contains(workflow.Name);
                                        break;
                                    case 1: //Dialog
                                        excluded = config.Excluded.Dialogs.Contains(workflow.Name);
                                        break;
                                    case 2: //BusinessRule
                                        excluded = config.Excluded.BusinessRules.Contains($"{workflow.Name}.[{workflow.PrimaryEntity}]");
                                        break;
                                    case 3: //Action
                                        excluded = config.Excluded.Actions.Contains(workflow.UniqueName);
                                        break;
                                    case 4: //BusinessProcessFlow
                                        excluded = config.Excluded.BusinessProcessFlows.Contains(workflow.UniqueName);
                                        break;
                                    case 5: //ModernFlow
                                        excluded = config.Excluded.ModernFlows.Contains(workflow.Name);
                                        break;
                                    case 6: //DesktopFlow
                                        excluded = config.Excluded.DesktopFlows.Contains(workflow.Name);
                                        break;
                                }
                                if (excluded)
                                {
                                    Console.WriteLine($"INFO: excluded '{workflow.Name}' ({TranslateCategory(workflow.Category)}); owned by {owner.DomainName}");
                                    continue;
                                }
                            }
                            result = UpdateOwner(workflow, assignee.SystemUserId) && result;
                            break;
                        }
                    }

                    if (psDebug) Console.WriteLine($"checked '{workflow.Name}' ({TranslateCategory(workflow.Category)})");
                }
            }
            return result;
        }

        private static string TranslateCategory(int category)
        {
            switch (category)
            {
                case 0:
                    return "Workflow";
                case 1:
                    return "Dialog";
                case 2:
                    return "Business Rule";
                case 3:
                    return "Action";
                case 4:
                    return "Business Process Flow";
                case 5:
                    return "Modern Flow";
                case 6:
                    return "Desktop Flow";
                default:
                    return $"Cat: {category}";
            }
        }

        private static bool UpdateOwner(Workflow workflow, Guid ownerId)
        {
            var result = true;
            var url = $"{PsAutomation.ApiUrl}/workflows({workflow.WorkflowId:D})";
            if (workflow.StateCode == 1)//is active and owner update needed
            {
                //MS; nasty exception -> Cannot update a published workflow definition.
                if (workflow.Category == 3)//Actions; it ain't get boring
                {
                    result = ProcessActivationJob.DeactivateXml($"{PsAutomation.DynamicsUrl}XRMServices/2011/Organization.svc/web", workflow);
                }
                else
                {
                    result = ProcessActivationJob.Deactivate(url, workflow);
                    if (result && workflow.Category == 5)//ModernFlow
                    {
                        result = DeleteCallbackRegistration(workflow);
                    }
                }
            }
            if (!result) return false;
            var owner = new StringBuilder().OpenJson();
            owner.AppendValue("ownerid@odata.bind", $"/systemusers({ownerId:D})");
            owner.CloseJson();
            //update owner
            Console.WriteLine($"INFO: update owner of '{workflow.Name}' ({TranslateCategory(workflow.Category)})");
            result = Client.Patch(url, owner.ToString());
            if (workflow.StateCode == 1 && result)//was active and owner update succeeded
            {
                result = ProcessActivationJob.Activate(url, workflow);
            }
            return result;
        }

        private static bool GetSystemUsers(out ODataContents<List<SystemUser>> content)
        {
            var roles = $"{PsAutomation.ApiUrl}/systemusers?$select=systemuserid,domainname,applicationid,fullname";
            var result = Client.Fetch<ODataContents<List<SystemUser>>>(roles, out var response);
            if (response.StatusCode == 200)
            {
                content = (ODataContents<List<SystemUser>>)response.Content;
            }
            else
            {
                content = new ODataContents<List<SystemUser>>();
            }
            return result;
        }

        private static bool Get_SYSTEM_UserId(out Guid systemUserId)
        {
            var roles = $"{PsAutomation.ApiUrl}/organizations?$select=systemuserid";
            var result = Client.Fetch<ODataContents<List<Organization>>>(roles, out var response);
            if (response.StatusCode == 200)
            {
                var organization = ((ODataContents<List<Organization>>)response.Content).Value.First();
                systemUserId = organization.SystemUserId;
            }
            else
            {
                systemUserId = Guid.Empty;
            }
            return result;
        }

        //known issue with callbackregistration
        private static bool DeleteCallbackRegistration(Workflow workflow)
        {
            var callbackregistrations = $"{PsAutomation.ApiUrl}/callbackregistrations?$select=callbackregistrationid&$filter={Uri.EscapeDataString($"name eq '{workflow.WorkflowId:D}'")}";
            var result = Client.Fetch<ODataContents<List<CallbackRegistration>>>(callbackregistrations, out var response);
            if (response.StatusCode != 200) return result;
            var content = (ODataContents<List<CallbackRegistration>>)response.Content;
            if (content.Value == null || content.Value.Count < 1) return true;
            foreach (var callbackregistration in content.Value)
            {
                result = Client.Delete($"{PsAutomation.ApiUrl}/callbackregistrations({callbackregistration.CallbackRegistrationId:D})") && result;
            }
            return result;
        }
    }
}
