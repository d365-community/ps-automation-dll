using System.Runtime.Serialization;

namespace D365.Community.Ps.Automation.Contract.Content
{
    [DataContract]
    internal sealed class EntityKeyMetadata
    {
        [DataMember(Name = "LogicalName")]
        internal string LogicalName { get; set; }

        [DataMember(Name = "EntityLogicalName")]
        internal string EntityLogicalName { get; set; }

        [DataMember(Name = "EntityKeyIndexStatus")]
        internal string EntityKeyIndexStatus { get; set; }
    }
}
