using System.Collections.Generic;
using System.Runtime.Serialization;

namespace D365.Community.Ps.Automation.Contract.Assembly
{
    [DataContract]
    internal sealed class PluginType
    {
        [DataMember(Name = "FriendlyName")]
        internal string FriendlyName { get; set; }

        [DataMember(Name = "Plugin")]
        internal string Plugin { get; set; }

        [DataMember(Name = "Description")]
        internal string Description { get; set; }

        [DataMember(Name = "TypeName")]
        internal string TypeName { get; set; }

        [DataMember(Name = "IsWorkflowActivity")]
        internal bool IsWorkflowActivity { get; set; }

        [DataMember(Name = "WorkflowActivityGroupName")]
        internal string WorkflowActivityGroupName { get; set; }

        [DataMember(Name = "Steps")]
        internal List<SdkMessageProcessingStep> SdkMessageProcessingSteps { get; set; } = new List<SdkMessageProcessingStep>();

        //TODO: nice to have
        [DataMember(Name = "customapis", EmitDefaultValue = false, IsRequired = false)]
        internal List<string> CustomApis { get; set; } = new List<string>();
    }
}