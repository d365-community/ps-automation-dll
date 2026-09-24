using System;
using System.Collections.Generic;
using D365.Community.Ps.Automation.Contract.Content;

namespace D365.Community.Ps.Automation.Data
{
    internal static class DynamicsHelper
    {
        internal static bool Etc2Etn(out Dictionary<int, string> map)
        {
            map = new Dictionary<int, string>();
            var retrieveAllEntities = $"{PsAutomation.ApiUrl}/RetrieveAllEntities(EntityFilters='Entity',RetrieveAsIfPublished=true)?$select=LogicalName,ObjectTypeCode";//select does not work
            var result = Client.Get<AllEntities>(retrieveAllEntities, out var response);
            if (response.StatusCode != 200) return result;
            var content = (AllEntities)response.Content;
            if (content.EntityMetadata == null || content.EntityMetadata.Count < 1) return false;
            foreach (var metadata in content.EntityMetadata)
            {
                map.Add(metadata.ObjectTypeCode, metadata.LogicalName);
            }
            var psTrace = bool.Parse(Environment.GetEnvironmentVariable("D365_PS_TRACE") ?? "false");
            if (psTrace) Console.WriteLine($"Etc2Etn: '{map}'");
            return result;
        }

        internal static bool Etn2Etc(out Dictionary<string, int> map)
        {
            map = new Dictionary<string, int>();
            var retrieveAllEntities = $"{PsAutomation.ApiUrl}/RetrieveAllEntities(EntityFilters='Entity',RetrieveAsIfPublished=true)?$select=LogicalName,ObjectTypeCode";//select does not work
            var result = Client.Get<AllEntities>(retrieveAllEntities, out var response);
            if (response.StatusCode != 200) return result;
            var content = (AllEntities)response.Content;
            if (content.EntityMetadata == null || content.EntityMetadata.Count < 1) return false;
            foreach (var metadata in content.EntityMetadata)
            {
                map.Add(metadata.LogicalName, metadata.ObjectTypeCode);
            }
            var psTrace = bool.Parse(Environment.GetEnvironmentVariable("D365_PS_TRACE") ?? "false");
            if (psTrace) Console.WriteLine($"Etn2Etc: '{map}'");
            return result;
        }
    }
}
