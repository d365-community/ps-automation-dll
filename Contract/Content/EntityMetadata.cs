using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace D365.Community.Ps.Automation.Contract.Content
{
    [DataContract]
    internal sealed class EntityMetadata
    {
        [DataMember(Name = "MetadataId")]
        internal Guid MetadataId { get; set; }

        [DataMember(Name = "LogicalName")]
        internal string LogicalName { get; set; }

        [DataMember(Name = "ObjectTypeCode")]
        internal int ObjectTypeCode { get; set; }

        [DataMember(Name = "Keys")]
        internal List<EntityKeyMetadata> Keys { get; set; } = new List<EntityKeyMetadata>();

        [DataMember(Name = "DisplayName")]
        internal EntityDisplayNameMetadata DisplayName { get; set; }
    }
}
