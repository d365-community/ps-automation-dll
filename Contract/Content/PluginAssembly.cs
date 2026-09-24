using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace D365.Community.Ps.Automation.Contract.Content
{
    [DataContract]
    internal sealed class PluginAssembly : ODataContent
    {
        [DataMember(Name = "pluginassemblyid")]
        internal Guid PluginAssemblyId { get; set; }

        [DataMember(Name = "name")]
        internal string Name { get; set; }

        [DataMember(Name = "description")]
        internal string Description { get; set; }

        [DataMember(Name = "pluginassembly_plugintype")]
        internal List<PluginType> PluginTypes { get; set; } = new List<PluginType>();
    }
}
