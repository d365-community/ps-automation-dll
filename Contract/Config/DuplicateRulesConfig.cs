using System.Collections.Generic;
using System.Runtime.Serialization;

namespace D365.Community.Ps.Automation.Contract.Config
{
    [DataContract]
    internal sealed class DuplicateRules : Caller
    {
        [DataMember(Name = "unpublished")]
        internal List<string> Unpublished { get; set; } = new List<string>();

        [DataMember(Name = "published")]
        internal List<string> Published { get; set; } = new List<string>();
    }
}
