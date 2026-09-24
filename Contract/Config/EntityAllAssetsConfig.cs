using System.Collections.Generic;
using System.Runtime.Serialization;

namespace D365.Community.Ps.Automation.Contract.Config
{
    [DataContract]
    internal sealed class EntitiesAllAssets : Caller
    {
        [DataMember(Name = "solutions", IsRequired = false)]
        internal List<EntityAllAssets> ListOfEntityAllAssets { get; set; } = new List<EntityAllAssets>();
    }

    [DataContract]
    internal sealed class EntityAllAssets
    {
        [DataMember(Name = "solution", IsRequired = true)]
        internal string Solution { get; set; }

        [DataMember(Name = "strict", IsRequired = true)]
        internal bool Strict { get; set; }

        //regex
        [DataMember(Name = "whitelist", IsRequired = true)]
        internal List<string> WhiteList { get; set; }

        //regex
        [DataMember(Name = "blacklist", IsRequired = true)]
        internal List<string> BlackList { get; set; }
    }
}
