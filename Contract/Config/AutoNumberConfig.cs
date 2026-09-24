using System.Collections.Generic;
using System.Runtime.Serialization;

namespace D365.Community.Ps.Automation.Contract.Config
{
    [DataContract]
    internal sealed class AutoNumberFormats : Caller
    {
        [DataMember(Name = "auto_number_formats", IsRequired = true)]
        internal List<AutoNumberFormat> Formats { get; set; } = new List<AutoNumberFormat>();
    }

    [DataContract]
    internal sealed class AutoNumberFormat
    {
        [DataMember(Name = "entity", IsRequired = true)]
        internal string Entity { get; set; }

        [DataMember(Name = "attribute", IsRequired = true)]
        internal string Attribute { get; set; }

        [DataMember(Name = "format", IsRequired = true)]
        internal string Format { get; set; }
    }
}
