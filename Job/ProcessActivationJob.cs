using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using D365.Community.Ps.Automation.Contract.Config;
using D365.Community.Ps.Automation.Contract.Content;
using D365.Community.Ps.Automation.Service;

namespace D365.Community.Ps.Automation.Job
{
    internal static class ProcessActivationJob
    {
        internal static bool Execute(ProcessActivation config)
        {
            var result = GetSolutionIds(config.Solutions, out var solutionIds);
            if (!result) return false;

            result = GetProcessIds(solutionIds, out var processIds);
            if (!result) return false;

            //componentstate:published and type:definition
            var workflows = $"{PsAutomation.ApiUrl}/workflows?$select=workflowid,category,name,uniquename,statecode,statuscode,primaryentity,_ownerid_value,_owninguser_value,_owningteam_value&$filter={Uri.EscapeDataString("componentstate eq 0 and type eq 1")}&$orderby={Uri.EscapeDataString("name asc")}";
            result = Client.Fetch<ODataContents<List<Workflow>>>(workflows, out var response);
            if (response.StatusCode != 200) return result;
            var content = (ODataContents<List<Workflow>>)response.Content;
            if (content.Value == null || content.Value.Count < 1) return true;
            var modernFlows = new Dictionary<Workflow, bool>();
            foreach (var workflow in content.Value)
            {
                var state = 0;
                switch (workflow.Category)
                {
                    case 0: //Workflow
                        if (config.Disabled.Workflows.Contains(workflow.Name))
                        {
                            state = -1;
                        }
                        else if (config.Enabled.Workflows.Contains(workflow.Name) || processIds.Contains(workflow.WorkflowId))
                        {
                            state = 1;
                        }
                        break;
                    case 1: //Dialog
                        if (config.Disabled.Dialogs.Contains(workflow.Name))
                        {
                            state = -1;
                        }
                        else if (config.Enabled.Dialogs.Contains(workflow.Name) || processIds.Contains(workflow.WorkflowId))
                        {
                            state = 1;
                        }
                        break;
                    case 2: //BusinessRule

                        if (config.Disabled.BusinessRules.Contains($"{workflow.Name}.[{workflow.PrimaryEntity}]"))
                        {
                            state = -1;
                        }
                        else if (config.Enabled.BusinessRules.Contains($"{workflow.Name}.[{workflow.PrimaryEntity}]") || processIds.Contains(workflow.WorkflowId))
                        {
                            state = 1;
                        }
                        break;
                    case 3: //Action
                        if (config.Disabled.Actions.Contains(workflow.UniqueName))
                        {
                            state = -1;
                        }
                        else if (config.Enabled.Actions.Contains(workflow.UniqueName) || processIds.Contains(workflow.WorkflowId))
                        {
                            state = 1;
                        }
                        break;
                    case 4: //BusinessProcessFlow
                        if (config.Disabled.BusinessProcessFlows.Contains(workflow.UniqueName))
                        {
                            state = -1;
                        }
                        else if (config.Enabled.BusinessProcessFlows.Contains(workflow.UniqueName) || processIds.Contains(workflow.WorkflowId))
                        {
                            state = 1;
                        }
                        break;
                    case 5: //ModernFlow
                        if (config.Disabled.ModernFlows.Contains(workflow.Name))
                        {
                            state = -1;
                            modernFlows.Add(workflow, false);
                        }
                        else if (config.Enabled.ModernFlows.Contains(workflow.Name) || processIds.Contains(workflow.WorkflowId))
                        {
                            state = 1;
                            modernFlows.Add(workflow, true);
                        }
                        break;
                    case 6: //DesktopFlow
                        if (config.Disabled.DesktopFlows.Contains(workflow.Name))
                        {
                            state = -1;
                        }
                        else if (config.Enabled.DesktopFlows.Contains(workflow.Name) || processIds.Contains(workflow.WorkflowId))
                        {
                            state = 1;
                        }
                        break;
                    default:
                        continue;
                }

                var patch = $"{PsAutomation.ApiUrl}/workflows({workflow.WorkflowId:D})";
                if (state < 0 && workflow.StateCode == 1) //Activated
                {
                    //MS; nasty exception -> Cannot update a published workflow definition.
                    if (workflow.Category == 3)//Actions; it ain't get boring
                    {
                        result = DeactivateXml($"{PsAutomation.DynamicsUrl}XRMServices/2011/Organization.svc/web", workflow) && result;
                    }
                    else
                    {
                        result = Deactivate(patch, workflow) && result;
                    }
                }
                else if (state > 0 && workflow.StateCode == 0) //Draft
                {
                    result = Activate(patch, workflow) && result;
                }
                else if (state > 0 && workflow.StateCode == 0) //Draft
                {
                    result = Activate(patch, workflow) && result;
                }
                //else StateCode == 2 //Suspended
            }

            if (!modernFlows.Any() || !result) return result;

            result = GetCallbackRegistrations(out var crs);
            if (!result) return false;
            foreach (var modernFlow in modernFlows)
            {
                var name = modernFlow.Key.Name;
                var workflowId = modernFlow.Key.WorkflowId;
                var enabled = modernFlow.Value;

                var count = crs.Count(e => e.Name == workflowId.ToString("D"));
                if (count > 1)
                {
                    Console.Error.WriteLine($"ERROR: modern flow '{name}' ({workflowId:D}) has multiple callback registrations!");
                    result = false;
                }

                switch (enabled)
                {
                    case true when count == 0:
                        {
                            var workflow = $"{PsAutomation.ApiUrl}/workflows({workflowId:D})?$select=clientdata";
                            result = Client.Get<Workflow>(workflow, out var res);
                            if (res.StatusCode != 200) return result;
                            var clientData = ((Workflow)res.Content).ClientData;
                            var type = Serializer.JsonDeserialize<ClientData>(clientData).Properties?.Definition?.Triggers?.First().Value?.Type;
                            if ("OpenApiConnectionWebhook".Equals(type, StringComparison.InvariantCultureIgnoreCase))
                            {
                                Console.Error.WriteLine($"ERROR: callback registration for modern flow '{name}' ({workflowId:D}) not found. This flow does not trigger!");
                                result = false;
                            }
                            else
                            {
                                Console.WriteLine($"INFO: callback registration for modern flow '{name}' of type '{type}' skipped ...");
                            }
                        }
                        break;
                    case false when count == 1:
                        {
                            var cr = crs.First(e => e.Name == workflowId.ToString("D"));
                            if (cr.SoftDeleteStatus == 0)//0 = On
                            {
                                Console.Error.WriteLine($"ERROR: modern flow '{name}' ({workflowId:D}) has still an active callback registration!");
                                result = false;
                            }
                        }
                        break;
                    case true when count == 1:
                        {
                            var cr = crs.First(e => e.Name == workflowId.ToString("D"));
                            if (cr.SoftDeleteStatus == 1)//1 = Off
                            {
                                Console.Error.WriteLine($"ERROR: modern flow '{name}' ({workflowId:D}) has still an inactive callback registration!");
                                result = false;
                            }
                            Console.WriteLine($"INFO: callback registration for modern flow '{name}' checked ...");
                        }
                        break;
                }
            }

            return result;
        }

        internal static bool Activate(string url, Workflow workflow)
        {
            var body = new StringBuilder().OpenJson();
            body.AppendValue("statecode", 1);
            body.AppendValue("statuscode", 2);
            body.CloseJson();
            Console.WriteLine($"INFO: activate '{workflow.Name}' ({TranslateCategory(workflow.Category)})");
            return Client.Patch(url, body.ToString());
        }

        internal static bool Deactivate(string url, Workflow workflow)
        {
            var body = new StringBuilder().OpenJson();
            body.AppendValue("statecode", 0);
            body.AppendValue("statuscode", 1);
            body.CloseJson();
            Console.WriteLine($"INFO: deactivate '{workflow.Name}' ({TranslateCategory(workflow.Category)})");
            return Client.Patch(url, body.ToString());
        }

        [Obsolete]
        internal static bool DeactivateXml(string url, Workflow workflow)
        {
            var body = new StringBuilder();
            body.Append("<s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\">");
            body.Append(" <s:Body>");
            body.Append("  <Execute xmlns=\"http://schemas.microsoft.com/xrm/2011/Contracts/Services\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\">");
            body.Append("   <request i:type=\"b:SetStateRequest\" xmlns:a=\"http://schemas.microsoft.com/xrm/2011/Contracts\" xmlns:b=\"http://schemas.microsoft.com/crm/2011/Contracts\">");
            body.Append("    <a:Parameters xmlns:c=\"http://schemas.datacontract.org/2004/07/System.Collections.Generic\">");
            body.Append("     <a:KeyValuePairOfstringanyType>");
            body.Append("      <c:key>EntityMoniker</c:key>");
            body.Append("      <c:value i:type=\"a:EntityReference\">");
            body.Append("       <a:Id>").Append(workflow.WorkflowId.ToString("D")).Append("</a:Id>");
            body.Append("       <a:LogicalName>workflow</a:LogicalName>");
            body.Append("       <a:Name i:nil=\"true\"/>");
            body.Append("      </c:value>");
            body.Append("     </a:KeyValuePairOfstringanyType>");
            body.Append("     <a:KeyValuePairOfstringanyType>");
            body.Append("      <c:key>State</c:key>");
            body.Append("      <c:value i:type=\"a:OptionSetValue\">");
            body.Append("       <a:Value>0</a:Value>");
            body.Append("      </c:value>");
            body.Append("     </a:KeyValuePairOfstringanyType>");
            body.Append("     <a:KeyValuePairOfstringanyType>");
            body.Append("      <c:key>Status</c:key>");
            body.Append("      <c:value i:type=\"a:OptionSetValue\">");
            body.Append("       <a:Value>1</a:Value>");
            body.Append("      </c:value>");
            body.Append("     </a:KeyValuePairOfstringanyType>");
            body.Append("    </a:Parameters>");
            body.Append("    <a:RequestId i:nil=\"true\"/>");
            body.Append("    <a:RequestName>SetState</a:RequestName>");
            body.Append("   </request>");
            body.Append("  </Execute>");
            body.Append(" </s:Body>");
            body.Append("</s:Envelope>");
            Console.WriteLine($"INFO: deactivate '{workflow.Name}' ({TranslateCategory(workflow.Category)})");
            return Client.PatchXml(url, body.ToString());
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

        private static bool GetSolutionIds(List<string> uniquenames, out List<Guid> solutionIds)
        {
            solutionIds = new List<Guid>();
            var result = true;
            foreach (var uniquename in uniquenames)
            {
                result = GetSolutionIds(uniquename, ref solutionIds) && result;
            }
            return result;
        }

        private static bool GetSolutionIds(string uniquename, ref List<Guid> solutionIds)
        {
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
                solutionIds.AddRange(content.Value.Select(solution => solution.SolutionId));
            }
            return result;
        }

        private static bool GetProcessIds(List<Guid> solutionIds, out List<Guid> processIds)
        {
            processIds = new List<Guid>();
            var result = true;
            foreach (var solutionId in solutionIds)
            {
                result = GetProcessIds(solutionId, ref processIds) && result;
            }
            return result;
        }

        private static bool GetProcessIds(Guid solutionId, ref List<Guid> processIds)
        {
            var processes = $"{PsAutomation.ApiUrl}/solutions({solutionId:D})/solution_solutioncomponent?$select=objectid&$filter={Uri.EscapeDataString("componenttype eq 29")}";
            var result = Client.Fetch<ODataContents<List<SolutionComponent>>>(processes, out var response);
            if (response.StatusCode == 200)
            {
                var content = (ODataContents<List<SolutionComponent>>)response.Content;
                if (content.Value == null || content.Value.Count < 1) return true;//does not contain processes
                processIds.AddRange(content.Value.Select(solutionComponent => solutionComponent.ObjectId));
            }
            return result;
        }


        //known issue with callbackregistration
        private static bool GetCallbackRegistrations(out List<CallbackRegistration> crs)
        {
            crs = null;
            var callbackregistrations = $"{PsAutomation.ApiUrl}/callbackregistrations?$select=name,softdeletestatus";
            var result = Client.Fetch<ODataContents<List<CallbackRegistration>>>(callbackregistrations, out var response);
            if (response.StatusCode != 200) return result;
            var content = (ODataContents<List<CallbackRegistration>>)response.Content;
            if (content.Value != null)
            {
                crs = content.Value;
                return true;
            }
            return false;
        }
    }
}
