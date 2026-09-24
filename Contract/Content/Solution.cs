using System;
using System.Runtime.Serialization;

namespace D365.Community.Ps.Automation.Contract.Content
{
    [DataContract]
    internal sealed class Solution : ODataContent
    {
        [DataMember(Name = "solutionid")]
        internal Guid SolutionId { get; set; }

        [DataMember(Name = "uniquename")]
        internal string UniqueName { get; set; }
    }
}
