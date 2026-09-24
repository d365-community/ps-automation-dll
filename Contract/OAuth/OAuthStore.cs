using System.Collections.Generic;
using System.Runtime.Serialization;

namespace D365.Community.Ps.Automation.Contract.OAuth
{
    [DataContract(Name = "OAuthStore")]
    internal class OAuthStore
    {
        [DataMember(Name = "Data")]
        internal Dictionary<string, OAuthRecord> Data { get; set; } = new Dictionary<string, OAuthRecord>();
    }
}
