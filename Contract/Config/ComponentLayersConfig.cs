using System.Collections.Generic;
using System.Runtime.Serialization;

namespace D365.Community.Ps.Automation.Contract.Config
{
    [DataContract]
    internal sealed class ComponentLayers : Caller
    {
        [DataMember(Name = "layers", IsRequired = false)]
        internal List<ComponentLayer> Layers { get; set; } = new List<ComponentLayer>();
    }

    [DataContract]
    internal sealed class ComponentLayer
    {
        [DataMember(Name = "types", IsRequired = true)]
        internal List<string> Types { get; set; }

        [DataMember(Name = "solution", IsRequired = false)]
        internal string Solution { get; set; }
    }
}
