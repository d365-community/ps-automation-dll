using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace D365.Community.Ps.Automation.Contract.Config
{
    [DataContract]
    internal sealed class ProcessOwner : Caller
    {
        [DataMember(Name = "whitelisted_domainnames")]
        internal List<string> WhitelistedDomainNames { get; set; } = new List<string>();

        [DataMember(Name = "domain_owner_map")]
        internal Dictionary<string, string> DomainOwnerMap { get; set; } = new Dictionary<string, string>();

        [DataMember(Name = "application_owner_map")]
        internal Dictionary<Guid, string> ApplicationMap { get; set; } = new Dictionary<Guid, string>();

        [DataMember(Name = "allow_all_owner")]
        internal bool AllowAllOwner { get; set; } = false;

        [DataMember(Name = "excluded")]
        internal ProcessOwnerTypes Excluded { get; set; }
    }

    [DataContract]
    internal sealed class ProcessOwnerTypes
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
