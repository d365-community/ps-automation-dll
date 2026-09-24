using System;
using System.Linq;
using D365.Community.Ps.Automation.Contract.Content;

namespace D365.Community.Ps.Automation.Job
{
    /// <summary>
    /// Verify if the alternate keys are successfully applied. The easiest way is to retrieve all entities with attributes.
    /// </summary>
    internal static class EntityKeyIndexStatusVerification
    {
        /// <summary>
        /// find possible issues in entity alternate keys<br/><br/>
        /// see https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/reference/retrieveallentities?view=dataverse-latest
        /// </summary>
        /// <returns>false, if any index creation failed, otherwise true</returns>
        internal static bool Execute()
        {
            var retrieveAllEntities = $"{PsAutomation.ApiUrl}/RetrieveAllEntities(EntityFilters='Attributes',RetrieveAsIfPublished=true)";
            var result = Client.Get<AllEntities>(retrieveAllEntities, out var response);
            if (response.StatusCode == 200)
            {
                var content = (AllEntities)response.Content;
                if (content.EntityMetadata == null || content.EntityMetadata.Count < 1) return false;
                var entities = content.EntityMetadata.Where(e => e.Keys != null && e.Keys.Count > 1).OrderBy(e => e.LogicalName).ToList();
                foreach (var entity in entities)
                {
                    foreach (var key in entity.Keys)
                    {
                        switch (key.EntityKeyIndexStatus)
                        {
                            case "Failed":
                                result = false;
                                Console.Error.WriteLine($"ERROR: Entity:{entity.LogicalName}; Key:{key.LogicalName}; Status:{key.EntityKeyIndexStatus}");
                                break;
                            default:
                                Console.WriteLine($"INFO: Entity:{entity.LogicalName}; Key:{key.LogicalName}; Status:{key.EntityKeyIndexStatus}");
                                break;
                        }
                    }
                }
            }
            return result;
        }
    }
}
