using System;
using System.Runtime.Serialization;

namespace D365.Community.Ps.Automation.Contract.Content
{
    [DataContract]
    internal sealed class EntityAttribute : IODataContent
    {
        [DataMember(Name = "@odata.context", IsRequired = true)]
        internal string Context { get; set; }

        [DataMember(Name = "MetadataId")]
        internal Guid MetadataId { get; set; }

        [DataMember(Name = "LogicalName")]
        internal string LogicalName { get; set; }

        [DataMember(Name = "AutoNumberFormat")]
        internal string AutoNumberFormat { get; set; }
    }
}
