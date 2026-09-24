using System.Collections.Generic;
using System.Runtime.Serialization;

namespace D365.Community.Ps.Automation.Contract.Config
{
    [DataContract]
    internal sealed class SavedQueries : Caller
    {
        [DataMember(Name = "filter", IsRequired = false, EmitDefaultValue = false)]
        internal string Filter { get; set; } = string.Empty;

        [DataMember(Name = "disabled_outlook_templates", IsRequired = false)]
        internal List<string> DisabledOutlookTemplates { get; set; } = new List<string>();

        [DataMember(Name = "outlook_templates", IsRequired = false)]
        internal List<OutlookTemplate> OutlookTemplates { get; set; } = new List<OutlookTemplate>();
    }

    [DataContract]
    internal sealed class OutlookTemplate
    {
        [DataMember(Name = "name", IsRequired = true)]
        internal string Name { get; set; }

        [DataMember(Name = "fetch_xml", IsRequired = true)]
        internal string FetchXml { get; set; }

        [DataMember(Name = "entity", IsRequired = true)]
        internal string Entity { get; set; }

        [DataMember(Name = "is_default", IsRequired = true)]
        internal bool IsDefault { get; set; }

        [DataMember(Name = "description", IsRequired = true)]
        internal string Description { get; set; }

        [DataMember(Name = "statecode", IsRequired = true)]
        internal int StateCode { get; set; }
    }
}
