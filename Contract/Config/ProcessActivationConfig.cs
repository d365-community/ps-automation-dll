using System.Collections.Generic;
using System.Runtime.Serialization;

namespace D365.Community.Ps.Automation.Contract.Config
{
    [DataContract]
    internal sealed class ProcessActivation : Caller
    {
        [DataMember(Name = "disabled")]
        internal ProcessActivationTypes Disabled { get; set; }

        [DataMember(Name = "enabled")]
        internal ProcessActivationTypes Enabled { get; set; }

        [DataMember(Name = "solutions")]
        internal List<string> Solutions { get; set; } = new List<string>();
    }

    [DataContract]
    internal sealed class ProcessActivationTypes
    {
        [DataMember(Name = "workflows")]
        internal List<string> Workflows { get; set; } = new List<string>();

        [DataMember(Name = "dialogs")]
        internal List<string> Dialogs { get; set; } = new List<string>();

        [DataMember(Name = "business_rules")]
        internal List<string> BusinessRules { get; set; } = new List<string>();

        [DataMember(Name = "actions")]
        internal List<string> Actions { get; set; } = new List<string>();

        [DataMember(Name = "business_process_flows")]
        internal List<string> BusinessProcessFlows { get; set; } = new List<string>();

        [DataMember(Name = "modern_flows")]
        internal List<string> ModernFlows { get; set; } = new List<string>();

        [DataMember(Name = "desktop_flows")]
        internal List<string> DesktopFlows { get; set; } = new List<string>();
    }
}
