using System;
using System.Runtime.Serialization;

namespace D365.Community.Ps.Automation.Contract.Content
{
    [DataContract]
    internal sealed class BusinessUnit : ODataContent
    {
        [DataMember(Name = "businessunitid")]
        internal Guid BusinessUnitId { get; set; }

        [DataMember(Name = "name")]
        internal string Name { get; set; }

        [DataMember(Name = "_parentbusinessunitid_value")]
        internal Guid? ParentBusinessUnitId { get; set; }
    }
}
