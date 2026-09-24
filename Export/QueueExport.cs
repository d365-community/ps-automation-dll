using D365.Community.Ps.Automation.Contract.Config;
using D365.Community.Ps.Automation.Contract.Content;
using D365.Community.Ps.Automation.Service;
using System;
using System.Collections.Generic;
using System.IO;

namespace D365.Community.Ps.Automation.Export
{
    internal static class QueueExport
    {
        internal static bool Execute(string directory, string prefix, string filter)
        {
            var queryFilter = string.IsNullOrWhiteSpace(filter) ? "" : Uri.EscapeDataString($" and ({filter})");
            var queues = $"{PsAutomation.ApiUrl}/queues?$select=queueid,name,description,queueviewtype,outgoingemaildeliverymethod,incomingemaildeliverymethod,incomingemailfilteringmethod&$filter={Uri.EscapeDataString("not startswith(name,'<') and not endswith(name,'>')")}{queryFilter}&$orderby={Uri.EscapeDataString("name asc")}";
            var result = Client.Fetch<ODataContents<List<Contract.Content.Queue>>>(queues, out var response);
            if (response.StatusCode != 200) return result;
            var content = (ODataContents<List<Contract.Content.Queue>>)response.Content;
            if (content.Value == null || content.Value.Count < 1) return true;

            var cfg = new Queues
            {
                CallerObjectId = PsAutomation.CallerObjectId,
                MsCrmCallerId = PsAutomation.MsCrmCallerId,
                Filter = filter
            };

            foreach (var queue in content.Value)
            {
                cfg.ListOfQueues.Add(new Contract.Config.Queue
                {
                    QueueId = queue.QueueId,
                    Name = queue.Name,
                    Description = queue.Description,
                    QueueViewType = queue.QueueViewType,
                    OutgoingEmailDeliveryMethod = queue.OutgoingEmailDeliveryMethod,
                    IncomingEmailDeliveryMethod = queue.IncomingEmailDeliveryMethod,
                    IncomingEmailFilteringMethod = queue.IncomingEmailFilteringMethod
                });
            }

            File.WriteAllText(Path.Combine(directory, $"{prefix}queue.json"), Serializer.JsonSerialize(cfg));

            return result;
        }
    }
}
