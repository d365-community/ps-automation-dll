using D365.Community.Ps.Automation.Contract.Content;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Text;
using D365.Community.Ps.Automation.Service;
using System.Globalization;
using System.Threading;
using System.Web;

namespace D365.Community.Ps.Automation.Job
{
    /// <summary>
    /// Manage "System Job" (asyncoperations) table by operation type "Bulk Delete" (13) and "Recurrence Start" (recurrencestarttime) given.
    /// </summary>
    internal static class BulkDeleteJob
    {
        /// <summary>
        /// manage bulk deletes<br/><br/>
        /// see https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/reference/asyncoperation?view=dataverse-latest
        /// </summary>
        /// <returns>false, if any index creation failed, otherwise true</returns>
        internal static bool Execute(Contract.Config.BulkDeletes deletes)
        {
            var asyncoperations = $"{PsAutomation.ApiUrl}/asyncoperations?$select=name,data,recurrencepattern,recurrencestarttime&$filter={Uri.EscapeDataString("operationtype eq 13 and recurrencestarttime ne null")}&$orderby={Uri.EscapeDataString("name asc")}";
            var result = Client.Fetch<ODataContents<List<AsyncOperation>>>(asyncoperations, out var response);
            if (response.StatusCode != 200) return result;

            var content = (ODataContents<List<AsyncOperation>>)response.Content;
            if (content.Value == null || content.Value.Count < 1) return true;
            var operations = content.Value;

            //find all bulk deletes which should be disabled
            Console.WriteLine("INFO: disable check");
            foreach (var disable in operations.Where(e => deletes.Deletes.Any(c => e.Name == c.Name && c.Disable)))
            {
                if (!string.IsNullOrEmpty(disable.RecurrencePattern))
                {
                    //set to run once, delete afterwards, nice hack
                    var typeBody = new StringBuilder()
                        .OpenJson()
                        .AppendValue("recurrencepattern", "")
                        .AppendValue("recurrencestarttime", DateTime.UtcNow.Add(TimeSpan.FromMinutes(10)))
                        .CloseJson();
                    result = Client.Patch($"{PsAutomation.ApiUrl}/asyncoperations({disable.AsyncOperationId:D})", typeBody.ToString()) && result;
                }
                else
                {
                    Console.WriteLine($"INFO: bulk delete '{disable.Name}' already disabled");
                }
            }

            //find bulk deletes which need to be updated
            Console.WriteLine("INFO: update check");
            foreach (var xupdate in operations.Where(e => deletes.Deletes.Any(c => e.Name == c.Name && !c.Disable)))
            {
                var update = xupdate;
                var bulkDelete = deletes.Deletes.Single(e => e.Name == update.Name);
                //change owner not supported
                if (!bulkDelete.RecurrencePattern.Equals(update.RecurrencePattern) || !bulkDelete.RecurrenceStartTime.Equals($"{update.RecurrenceStartTime:HH:mm}"))
                {
                    Console.WriteLine($"INFO: update bulk delete '{update.Name}'; StartTime:{bulkDelete.RecurrenceStartTime}; Pattern:{bulkDelete.RecurrencePattern}!");
                    Console.WriteLine("WARNING: update of fetch-xml is neither checked nor supported!");
                    var startTime = DateTime.ParseExact($"{DateTime.UtcNow.AddDays(1):yyyy-MM-dd} {bulkDelete.RecurrenceStartTime}:00 UTC", "yyyy-MM-dd HH:mm:ss UTC", CultureInfo.CurrentCulture, DateTimeStyles.AssumeUniversal).ToUniversalTime();
                    if (string.IsNullOrEmpty(update.RecurrencePattern))
                    {
                        //disabled once cannot be reactivated, but copied
                        Console.WriteLine("INFO: disabled once cannot be reactivated, but copied!");
                        var data = update.Data;
                        var start = data.IndexOf("<string>&lt;fetch", StringComparison.InvariantCultureIgnoreCase) + 8;
                        var end = data.IndexOf("fetch&gt;</string>", StringComparison.InvariantCultureIgnoreCase) + 9;
                        var fetchXml = data.Substring(start, end - start).Replace("&lt;", "<").Replace("&gt;", ">");
                        var fetchXmlToQueryExpression = $"{PsAutomation.ApiUrl}/FetchXmlToQueryExpression(FetchXml=@p1)?@p1='{HttpUtility.UrlEncode(fetchXml)}'";
                        result = Client.Get<QueryExpression>(fetchXmlToQueryExpression, out var queryExpression) && result;
                        if (queryExpression.StatusCode == 200)
                        {
                            if (queryExpression.Content == null) return false;
                            var query = ((QueryExpression)queryExpression.Content).Query;
                            var bulkDeleteBody = new StringBuilder()
                                .OpenJson()
                                .AppendValue("JobName", update.Name)
                                .AppendValue("QuerySet", new List<object> { Serializer.JsonSerialize(query, false) })
                                .AppendValue("StartDateTime", startTime)
                                .AppendValue("RecurrencePattern", bulkDelete.RecurrencePattern)
                                .AppendValue("SendEmailNotification", false)
                                .AppendValue("ToRecipients", new List<object>())
                                .AppendValue("CCRecipients", new List<object>())
                                .AppendValue("RunNow", false)
                                .CloseJson();
                            //create copy
                            if (Client.Post($"{PsAutomation.ApiUrl}/BulkDelete()", bulkDeleteBody.ToString(), out _))
                            {
                                Console.WriteLine("INFO: wait ~65sec!");
                                Thread.Sleep(65000);//TODO: does not work stable; -> delete failed {id}: Cannot update job because it is not a recurring job.
                                Console.WriteLine($"INFO: bulk delete '{update.Name}' copied!");
                                Console.WriteLine($"INFO: delete old bulk delete '{update.Name}'!");
                                result = Client.Delete($"{PsAutomation.ApiUrl}/asyncoperations({update.AsyncOperationId:D})") && result;
                            }
                            else
                            {
                                result = false;
                            }
                        }
                    }
                    else
                    {
                        var bulkDeleteBody = new StringBuilder()
                            .OpenJson()
                            .AppendValue("StartDateTime", startTime)
                            .AppendValue("RecurrencePattern", bulkDelete.RecurrencePattern)
                            .CloseJson();
                        //update
                        result = Client.Patch($"{PsAutomation.ApiUrl}/asyncoperations({update.AsyncOperationId:D})", bulkDeleteBody.ToString()) && result;
                    }
                }
                Console.WriteLine($"bulk delete '{update.Name}' checked!");
            }

            //find bulk deletes which need to be created
            Console.WriteLine("INFO: create check");
            foreach (var create in deletes.Deletes.Where(e => operations.All(c => e.Name != c.Name)))
            {
                if (create.Disable)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(create.QueryExpression))
                {
                    Console.Error.WriteLine($"ERROR: bulk delete '{create.Name}' has no query ...");
                    result = false;
                    continue;
                }

                var startTime = DateTime.ParseExact($"{DateTime.UtcNow.AddDays(1):yyyy-MM-dd} {create.RecurrenceStartTime}:00 UTC", "yyyy-MM-dd HH:mm:ss UTC", CultureInfo.CurrentCulture, DateTimeStyles.AssumeUniversal).ToUniversalTime();
                var bulkDeleteBody = new StringBuilder()
                    .OpenJson()
                    .AppendValue("JobName", create.Name)
                    .AppendValue("QuerySet", new List<object> { create.QueryExpression })
                    .AppendValue("StartDateTime", startTime)
                    .AppendValue("RecurrencePattern", create.RecurrencePattern)
                    .AppendValue("SendEmailNotification", false)
                    .AppendValue("ToRecipients", new List<object>())
                    .AppendValue("CCRecipients", new List<object>())
                    .AppendValue("RunNow", false)
                    .CloseJson();
                //create
                Console.WriteLine($"INFO: create bulk delete '{create.Name}'; StartTime:{create.RecurrenceStartTime}; Pattern:{create.RecurrencePattern}");
                result = Client.Post($"{PsAutomation.ApiUrl}/BulkDelete()", bulkDeleteBody.ToString(), out _) && result;
            }
            return result;
        }
    }
}
