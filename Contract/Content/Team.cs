using System;
using System.Runtime.Serialization;

namespace D365.Community.Ps.Automation.Contract.Content
{
    [DataContract]
    internal sealed class Team : ODataContent
    {
        [DataMember(Name = "teamid")]
        internal Guid TeamId { get; set; }

        [DataMember(Name = "name")]
        internal string Name { get; set; }
    }
}
