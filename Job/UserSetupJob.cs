using D365.Community.Ps.Automation.Contract.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using D365.Community.Ps.Automation.Service;

namespace D365.Community.Ps.Automation.Job
{
    internal static class UserSetupJob
    {
        internal static bool Execute(Contract.Config.UserSet userSet)
        {
            var result = GetRoles(out var roles);
            foreach (var userSetup in userSet.Users)
            {
                Console.WriteLine($"INFO: {userSetup}");
                //with roles and teams
                var systemusers = $"{PsAutomation.ApiUrl}/systemusers?$select=systemuserid,domainname,_businessunitid_value&$filter={Uri.EscapeDataString($"domainname eq '{userSetup.UserName}'")}&$expand=systemuserroles_association($select=roleid,name,_businessunitid_value;$filter={Uri.EscapeDataString("componentstate eq 0")}),teammembership_association($select=teamid,name;$filter={Uri.EscapeDataString("teamtype eq 0 or teamtype eq 1")})";
                result = Client.Fetch<ODataContents<List<SystemUser>>>(systemusers, out var response) && result;
                if (response.StatusCode != 200) continue;
                var content = (ODataContents<List<SystemUser>>)response.Content;
                if (content.Value == null || content.Value.Count < 1) continue;
                if (content.Value.Count > 1)
                {
                    result = false;
                    continue;
                }
                var user = content.Value[0];

                //1) Business Units
                if (user.BusinessUnitId != userSetup.BusinessUnitId)
                {
                    //user in wrong bu
                    var userBody = new StringBuilder()
                        .OpenJson()
                        .AppendValue("_businessunitid_value", userSetup.BusinessUnitId)
                        .CloseJson();
                    result = Client.Patch($"{PsAutomation.ApiUrl}/systemusers({user.SystemUserId:D})", userBody.ToString()) && result;
                }

                //2) Roles
                var assignedRoles = user.Roles;
                var expectedRoles = (from userRole in userSetup.UserRoles from securityRole in userRole.SecurityRoles select new Role { BusinessUnitId = userRole.BusinessUnitId, Name = securityRole }).ToList();

                var spareRoles = assignedRoles.Where(ar => expectedRoles.All(er => ar.Name != er.Name && ar.BusinessUnitId != er.BusinessUnitId));
                foreach (var role in spareRoles)
                {
                    Console.WriteLine($"INFO: Disassociate SecurityRole:{role.Name}");
                    result = Client.Delete($"{PsAutomation.ApiUrl}/systemusers({user.SystemUserId:D})/systemuserroles_association/$ref?$id={PsAutomation.ApiUrl}/roles({role.RoleId})") && result;
                }

                var missingRoles = expectedRoles.Where(er => assignedRoles.All(ar => er.Name != ar.Name && er.BusinessUnitId != ar.BusinessUnitId));
                foreach (var role in missingRoles)
                {
                    Console.WriteLine($"INFO: Associate SecurityRole:{role.Name}");
                    var associateBody = new StringBuilder()
                        .OpenJson()
                        .AppendValue("@odata.id", $"{PsAutomation.ApiUrl}/roles({role.RoleId})")
                        .CloseJson();
                    result = Client.Post($"{PsAutomation.ApiUrl}/systemusers({user.SystemUserId:D})/systemuserroles_association/$ref", associateBody.ToString(), out _) && result;
                }

                //3) Teams
                var assignedTeams = user.Teams;
                var expectedTeams = (from userTeam in userSetup.UserTeams select new Team { TeamId = userTeam.TeamId }).ToList();

                var spareTeams = assignedTeams.Where(at => expectedTeams.All(et => at.TeamId != et.TeamId));
                foreach (var team in spareTeams)
                {
                    Console.WriteLine($"INFO: Disassociate Team:{team.Name}");
                    result = Client.Delete($"{PsAutomation.ApiUrl}/systemusers({user.SystemUserId:D})/teammembership_association/$ref?$id={PsAutomation.ApiUrl}/teams({team.TeamId})") && result;
                }

                var missingTeams = expectedTeams.Where(et => assignedTeams.All(at => et.TeamId != at.TeamId));
                foreach (var team in missingTeams)
                {
                    Console.WriteLine($"INFO: Associate Team:{team.Name}");
                    var associateBody = new StringBuilder()
                        .OpenJson()
                        .AppendValue("@odata.id", $"{PsAutomation.ApiUrl}/teams({team.TeamId})")
                        .CloseJson();
                    result = Client.Post($"{PsAutomation.ApiUrl}/systemusers({user.SystemUserId:D})/teammembership_association/$ref", associateBody.ToString(), out _) && result;
                }
            }

            return result;
        }

        private static bool GetRoles(out ODataContents<List<Role>> content)
        {
            var roles = $"{PsAutomation.ApiUrl}/roles?$select=roleid,name,_businessunitid_value&$filter={Uri.EscapeDataString("componentstate eq 0")}";
            var result = Client.Fetch<ODataContents<List<Role>>>(roles, out var response);
            if (response.StatusCode == 200)
            {
                content = (ODataContents<List<Role>>)response.Content;
            }
            else
            {
                content = new ODataContents<List<Role>>();
            }

            return result;
        }
    }
}
