using System;
using System.Runtime.Serialization;

namespace D365.Community.Ps.Automation.Contract.Content
{
    [DataContract]
    internal sealed class DuplicateRule : ODataContent
    {
        [DataMember(Name = "duplicateruleid")]
        internal Guid DuplicateRuleId { get; set; }

        [DataMember(Name = "name")]
        internal string Name { get; set; }

        [DataMember(Name = "statuscode")]
        internal int? StatusCode { get; set; }
    }
}
