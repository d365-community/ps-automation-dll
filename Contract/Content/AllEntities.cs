using System.Collections.Generic;
using System.Runtime.Serialization;

namespace D365.Community.Ps.Automation.Contract.Content
{
    [DataContract]
    internal sealed class AllEntities : IODataContent
    {
        [DataMember(Name = "@odata.context")]
        internal string Context { get; set; }

        [DataMember(Name = "EntityMetadata")]
        internal List<EntityMetadata> EntityMetadata { get; set; } = new List<EntityMetadata>();
    }
}
