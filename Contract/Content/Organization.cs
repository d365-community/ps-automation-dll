using System;
using System.Runtime.Serialization;

namespace D365.Community.Ps.Automation.Contract.Content
{
    [DataContract]
    internal sealed class Organization : ODataContent
    {
        [DataMember(Name = "organizationid")]
        internal Guid OrganizationId { get; set; }

        [DataMember(Name = "name")]
        internal string Name { get; set; }

        [DataMember(Name = "systemuserid")]
        internal Guid SystemUserId { get; set; }
    }
}
