using D365.Community.Ps.Automation.Contract.Config;
using D365.Community.Ps.Automation.Contract.Content;
using System;
using System.Collections.Generic;

namespace D365.Community.Ps.Automation.Service
{
    internal static class CallerCheck
    {
        internal static bool Execute(Caller caller)
        {
            if (!string.IsNullOrWhiteSpace(caller.CallerObjectId) && !string.IsNullOrWhiteSpace(caller.MsCrmCallerId))
            {
                Console.Error.WriteLine("ERROR: MSCRMCallerID Xor CallerObjectId; remove one of the values");
                return false;
            }
            if (!string.IsNullOrWhiteSpace(caller.CallerObjectId))
            {
                if (Guid.TryParse(caller.CallerObjectId, out var guid))
                {
                    Console.WriteLine($"INFO: CallerObjectId is '{guid:D}'");
                    var systemusers = $"{PsAutomation.ApiUrl}/systemusers?$select=systemuserid,domainname,_businessunitid_value&$filter={Uri.EscapeDataString($"azureactivedirectoryobjectid eq '{guid:D}'")}";
                    var result = Client.Fetch<ODataContents<List<SystemUser>>>(systemusers, out var response);
                    if (response.StatusCode == 200)
                    {
                        var content = (ODataContents<List<SystemUser>>)response.Content;
                        if (content.Value?.Count != 1)
                        {
                            Console.Error.WriteLine($"ERROR: CallerObjectId '{caller.CallerObjectId}' is not a valid Guid!");
                            result = false;
                        }
                        else
                        {
                            PsAutomation.CallerObjectId = guid.ToString("D");
                            Console.WriteLine($"INFO: CallerObjectId is '{content.Value[0].FullName}'");
                        }
                    }
                    return result;

                }
                Console.Error.WriteLine($"ERROR: CallerObjectId '{caller.CallerObjectId}' is not a valid Guid!");
                return false;
            }
            if (!string.IsNullOrWhiteSpace(caller.MsCrmCallerId))
            {
                if (Guid.TryParse(caller.MsCrmCallerId, out var guid))
                {
                    Console.WriteLine($"INFO: MSCRMCallerID is '{guid:D}'");
                    var systemuser = $"{PsAutomation.ApiUrl}/systemusers({guid:D})?$select=systemuserid,domainname,applicationid,fullname";
                    var result = Client.Get<SystemUser>(systemuser, out var response);
                    if (response.StatusCode == 200)
                    {
                        PsAutomation.MsCrmCallerId = guid.ToString("D");
                        Console.WriteLine($"INFO: MSCRMCallerID is '{((SystemUser)response.Content).FullName}'");
                    }
                    return result;
                }
                Console.Error.WriteLine($"ERROR: MSCRMCallerID '{caller.MsCrmCallerId}' is not a valid Guid!");
                return false;
            }
            return true;
        }
    }
}
