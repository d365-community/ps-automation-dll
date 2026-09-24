using System;
using System.Runtime.Serialization;

namespace D365.Community.Ps.Automation.Contract.Content
{
    [DataContract]
    internal sealed class SolutionComponent : ODataContent
    {
        [DataMember(Name = "solutioncomponentid")]
        internal Guid SolutionComponentId { get; set; }

        [DataMember(Name = "objectid")]
        internal Guid ObjectId { get; set; }

        [DataMember(Name = "_solutionid_value")]
        internal Guid SolutionId { get; set; }

        [DataMember(Name = "componenttype")]
        internal int ComponentType { get; set; }

        [DataMember(Name = "rootcomponentbehavior")]
        internal int RootComponentBehavior { get; set; }

        [DataMember(Name = "ismetadata")]
        internal bool IsMetadata { get; set; }
    }
}
