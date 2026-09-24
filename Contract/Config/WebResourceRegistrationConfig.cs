using System.Collections.Generic;
using System.Runtime.Serialization;

namespace D365.Community.Ps.Automation.Contract.Config
{
    [DataContract]
    internal sealed class WebResourceRegistration : Caller
    {
        [DataMember(Name = "webresource_folder", IsRequired = true)]
        internal string WebResourceFolder { get; set; }

        [DataMember(Name = "webresource_publish", IsRequired = false)]
        internal bool WebResourcePublish { get; set; } = false;

        [DataMember(Name = "webresource_patterns", IsRequired = false)]
        internal List<string> WebResourcePatterns { get; set; }

        [DataMember(Name = "webresource_prefix", IsRequired = false)]
        internal string WebResourcePrefix { get; set; }

        [DataMember(Name = "webresource_solution", IsRequired = false)]
        internal string WebResourceSolution { get; set; }

        [DataMember(Name = "webresource_recursive", IsRequired = false)]
        internal bool WebResourceRecursive { get; set; } = false;

        [DataMember(Name = "webresource_keep_folder_structure", IsRequired = false)]
        internal bool WebResourceKeepFolderStructure { get; set; } = false;

        [DataMember(Name = "webresource_map", IsRequired = false)]
        internal Dictionary<string, string> WebResourceMap { get; set; } = new Dictionary<string, string>();
    }
}
