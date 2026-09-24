using System.Collections.Generic;
using System.Runtime.Serialization;

namespace D365.Community.Ps.Automation.Contract.Assembly
{
    [DataContract]
    internal sealed class PluginAssembly
    {
        [DataMember(Name = "Assembly")]
        internal string Assembly { get; set; }

        [DataMember(Name = "Description")]
        internal string Description { get; set; }

        [DataMember(Name = "Plugins")]
        internal List<PluginType> PluginTypes { get; set; } = new List<PluginType>();
    }
}
