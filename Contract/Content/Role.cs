using System;
using System.Runtime.Serialization;

namespace D365.Community.Ps.Automation.Contract.Content
{
    [DataContract]
    internal sealed class Role : ODataContent
    {
        [DataMember(Name = "roleid")]
        internal Guid RoleId { get; set; }

        [DataMember(Name = "name")]
        internal string Name { get; set; }

        [DataMember(Name = "_businessunitid_value")]
        internal Guid BusinessUnitId { get; set; }
    }
}
