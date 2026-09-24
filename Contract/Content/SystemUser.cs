using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace D365.Community.Ps.Automation.Contract.Content
{
    [DataContract]
    internal sealed class SystemUser : ODataContent
    {
        [DataMember(Name = "systemuserid")]
        internal Guid SystemUserId { get; set; }

        [DataMember(Name = "domainname")]
        internal string DomainName { get; set; }

        [DataMember(Name = "applicationid")]
        internal Guid? ApplicationId { get; set; }

        [DataMember(Name = "fullname")]
        internal string FullName { get; set; }

        [DataMember(Name = "_businessunitid_value")]
        internal Guid BusinessUnitId { get; set; }

        [DataMember(Name = "systemuserroles_association")]
        internal List<Role> Roles { get; set; } = new List<Role>();

        [DataMember(Name = "teammembership_association")]
        internal List<Team> Teams { get; set; } = new List<Team>();
    }
}
