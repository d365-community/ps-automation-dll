using System.Collections.Generic;
using System.Runtime.Serialization;

namespace D365.Community.Ps.Automation.Contract.Assembly
{
    [DataContract]
    internal sealed class SdkMessageProcessingStep
    {
        [DataMember(Name = "Step", IsRequired = true)]
        internal string Step { get; set; }

        [DataMember(Name = "Message", IsRequired = true)]
        internal string Message { get; set; }

        [DataMember(Name = "PrimaryEntity", IsRequired = true)]
        internal string PrimaryEntity { get; set; }

        [DataMember(Name = "SecondaryEntity", IsRequired = false, EmitDefaultValue = false)]
        internal string SecondaryEntity { get; set; }

        [DataMember(Name = "Description", IsRequired = false, EmitDefaultValue = false)]
        internal string Description { get; set; }

        [DataMember(Name = "ExecutionOrder", IsRequired = true)]
        internal int ExecutionOrder { get; set; }

        [DataMember(Name = "FilteringAttributes", IsRequired = false, EmitDefaultValue = false)]
        internal string FilteringAttributes { get; set; }

        [DataMember(Name = "AsyncAutoDelete", IsRequired = true)]
        internal bool AsyncAutoDelete { get; set; }

        [DataMember(Name = "Stage", IsRequired = true)]
        internal string Stage { get; set; }

        [DataMember(Name = "Mode", IsRequired = true)]
        internal string Mode { get; set; }

        [DataMember(Name = "UnsecureConfiguration", IsRequired = false, EmitDefaultValue = false)]
        internal string UnsecureConfiguration { get; set; }

        [DataMember(Name = "Status", IsRequired = true)]
        internal string Status { get; set; }

        [DataMember(Name = "Images")]
        internal List<SdkMessageProcessingStepImage> SdkMessageProcessingStepImages { get; set; }
    }
}