using System;
using System.Runtime.Serialization;

namespace D365.Community.Ps.Automation.Contract.Content
{
    [DataContract]
    internal sealed class Entity : ODataContent
    {
        [DataMember(Name = "entityid")]
        internal Guid EntityId { get; set; }

        [DataMember(Name = "name")]
        internal string Name { get; set; }

        [DataMember(Name = "logicalname")]
        internal string LogicalName { get; set; }

        [DataMember(Name = "logicalcollectionname")]
        internal string LogicalCollectionName { get; set; }

        [DataMember(Name = "originallocalizedname")]
        internal string OriginalLocalizedName { get; set; }
    }
}
