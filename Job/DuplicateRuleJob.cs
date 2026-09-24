using D365.Community.Ps.Automation.Contract.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace D365.Community.Ps.Automation.Job
{
    internal static class DuplicateRuleJob
    {
        internal static bool Execute(Contract.Config.DuplicateRules config)
        {
            var duplicaterules = $"{PsAutomation.ApiUrl}/duplicaterules?$select=duplicateruleid,name,statuscode";
            var result = Client.Fetch<ODataContents<List<DuplicateRule>>>(duplicaterules, out var response);
            if (response.StatusCode != 200) return result;

            var content = (ODataContents<List<DuplicateRule>>)response.Content;
            if (content.Value == null || content.Value.Count < 1) return true;
            var rules = content.Value;

            var loop = 1;
            while (rules.Any(r => r.StatusCode == 1))
            {
                Console.WriteLine($"INFO: ... still publishing (loop: {loop:D3}) ...");
                loop++;
                Thread.Sleep(Client.Random.Next(3 * 1000, 7 * 1000));//3-7sec
                result = Client.Fetch<ODataContents<List<DuplicateRule>>>(duplicaterules, out response) && result;
                if (response.StatusCode != 200) return result;
                content = (ODataContents<List<DuplicateRule>>)response.Content;
                if (content.Value == null || content.Value.Count < 1) break;
                rules = content.Value;
            }

            Console.WriteLine("INFO: unpublish check");
            foreach (var name in config.Unpublished)
            {
                var rule = rules.FirstOrDefault(r => string.Equals(r.Name, name, StringComparison.InvariantCultureIgnoreCase));
                if (rule == null)
                {
                    result = false;
                    Console.Error.WriteLine($"ERROR: Rule {name} missing!");
                }
                else if (rule.StatusCode != 0)
                {
                    //UnpublishDuplicateRule
                    result = Client.Post<NoContent>($"{PsAutomation.ApiUrl}/UnpublishDuplicateRule", $"{{ \"DuplicateRuleId\": \"{rule.DuplicateRuleId:D}\" }}", out var unpublishResponse) && result;
                    if (unpublishResponse.StatusCode == 204) continue;
                    result = false;
                    Console.Error.WriteLine($"ERROR(Http {unpublishResponse.StatusCode}): {((IODataError)unpublishResponse.Content).GetErrorMessage()}");
                    break;
                }
            }

            Console.WriteLine("INFO: publish check");
            foreach (var name in config.Published)
            {
                var rule = rules.FirstOrDefault(r => string.Equals(r.Name, name, StringComparison.InvariantCultureIgnoreCase));
                if (rule == null)
                {
                    result = false;
                    Console.Error.WriteLine($"ERROR: Rule {name} missing!");
                }
                else if (rule.StatusCode != 2)
                {
                    //PublishDuplicateRule
                    result = Client.Post<AsyncOperation>($"{PsAutomation.ApiUrl}/duplicaterules({rule.DuplicateRuleId:D})/Microsoft.Dynamics.CRM.PublishDuplicateRule", string.Empty, out var publishResponse) && result;
                    if (publishResponse.StatusCode != 200) return result;
                    var publishOperation = (AsyncOperation)publishResponse.Content;
                    WaitOnAsyncOperation(rule.Name, publishOperation, ref result);
                }
            }
            return result;
        }

        private static void WaitOnAsyncOperation(string rule, AsyncOperation asyncOperation, ref bool result)
        {
            var counter = 1;
            int? currentStatus;
            do
            {
                Console.WriteLine($"INFO: ... publishing {rule} ...) (counter: {counter:D3}) ...");
                counter++;
                Thread.Sleep(Client.Random.Next(3 * 1000, 7 * 1000));//3-7sec
                var asyncoperation = $"{PsAutomation.ApiUrl}/asyncoperations({asyncOperation.AsyncOperationId:D})?$select=name,statuscode,friendlymessage,message";
                result = Client.Get<AsyncOperation>(asyncoperation, out var record) && result;
                if (record.StatusCode == 200)
                {
                    currentStatus = ((AsyncOperation)record.Content).StatusCode;
                    // ReSharper disable once InvertIf
                    if (currentStatus == 31 || currentStatus == 32 || counter > 200)//31-Failed, 32-Canceled, endless loop
                    {
                        result = false;
                        Console.Error.WriteLine($"ERROR: {rule}");
                        break;
                    }
                }
                else
                {
                    result = false;
                    Console.Error.WriteLine($"ERROR(Http {record.StatusCode}): {((IODataError)record.Content).GetErrorMessage()}");
                    break;
                }
            } while (currentStatus != 30);
        }
    }
}
