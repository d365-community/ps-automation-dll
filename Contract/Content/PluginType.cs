using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace D365.Community.Ps.Automation.Contract.Content
{
    [DataContract]
    internal sealed class PluginType : ODataContent
    {
        [DataMember(Name = "plugintypeid")]
        internal Guid PluginTypeId { get; set; }

        [DataMember(Name = "friendlyname")]
        internal string FriendlyName { get; set; }

        [DataMember(Name = "name")]
        internal string Name { get; set; }

        [DataMember(Name = "description")]
        internal string Description { get; set; }

        [DataMember(Name = "typename")]
        internal string TypeName { get; set; }

        [DataMember(Name = "isworkflowactivity")]
        internal bool IsWorkflowActivity { get; set; }

        [DataMember(Name = "workflowactivitygroupname")]
        internal string WorkflowActivityGroupName { get; set; }

        [DataMember(Name = "plugintypeid_sdkmessageprocessingstep")]
        internal List<SdkMessageProcessingStep> SdkMessageProcessingSteps { get; set; } = new List<SdkMessageProcessingStep>();
    }
}