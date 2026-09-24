using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using D365.Community.Ps.Automation.Contract.Content;

namespace D365.Community.Ps.Automation.Job
{
    /// <summary>
    /// Verify if the index creation was successful. The easiest way is to query the "System Job" (asyncoperations) table by operation type "EntityKey Index Creation" (63) and status codes not equal to "Succeeded" (30).
    /// </summary>
    internal static class EntityKeyIndexCreationVerification
    {
        /// <summary>
        /// find possible issues in entity index creation<br/><br/>
        /// see https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/reference/asyncoperation?view=dataverse-latest
        /// </summary>
        /// <returns>false, if any index creation failed, otherwise true</returns>
        internal static bool Execute()
        {
            var asyncoperations = $"{PsAutomation.ApiUrl}/asyncoperations?$select=name,statuscode,friendlymessage,message&$filter={Uri.EscapeDataString("operationtype eq 63 and statuscode ne 30")}&$orderby={Uri.EscapeDataString("name asc")}";
            var result = Client.Fetch<ODataContents<List<AsyncOperation>>>(asyncoperations, out var response);
            if (response.StatusCode == 200)
            {
                var content = (ODataContents<List<AsyncOperation>>)response.Content;
                if (content.Value == null || content.Value.Count < 1) return true;
                var tasks = new List<Task>(content.Value.Count);
                foreach (var operation in content.Value)
                {
                    if (operation.StatusCode == 31 || operation.StatusCode == 32)//31-Failed, 32-Canceled
                    {
                        result = false;
                        Console.Error.WriteLine($"ERROR: {operation.Name} - {operation.FriendlyMessage}");
                        continue;
                    }
                    tasks.Add(Task.Run(() =>
                    {
                        var asyncoperation = $"{PsAutomation.ApiUrl}/asyncoperations({operation.AsyncOperationId:D})?$select=name,statuscode,friendlymessage,message";
                        Console.WriteLine($"INFO: {operation.Name}");
                        var counter = 1;
                        int? currentStatus;
                        do
                        {
                            Console.WriteLine($"INFO: ... wait on {operation.FriendlyMessage}({operation.AsyncOperationId:D}) (counter: {counter:D3}) ...");
                            counter++;
                            Thread.Sleep(Client.Random.Next(3 * 1000, 7 * 1000));//3-7sec
                            // ReSharper disable once AccessToModifiedClosure
                            result = Client.Get<AsyncOperation>(asyncoperation, out var record) && result;
                            if (record.StatusCode == 200)
                            {
                                currentStatus = ((AsyncOperation)record.Content).StatusCode;
                                // ReSharper disable once InvertIf
                                if (currentStatus == 31 || currentStatus == 32 || counter > 200)//31-Failed, 32-Canceled, endless loop
                                {
                                    result = false;
                                    Console.Error.WriteLine($"ERROR: {operation.Name} - {operation.FriendlyMessage}");
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
                    }));
                }
                Task.WaitAll(tasks.ToArray());
            }
            return result;
        }
    }
}
