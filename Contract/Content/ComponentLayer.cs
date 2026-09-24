using System;
using System.Runtime.Serialization;

namespace D365.Community.Ps.Automation.Contract.Content
{
    [DataContract]
    internal sealed class ComponentLayer : ODataContent
    {
        [DataMember(Name = "msdyn_componentlayerid")]
        internal Guid ComponentLayerId { get; set; }

        [DataMember(Name = "msdyn_solutioncomponentname")]
        internal string SolutionComponentName { get; set; }

        [DataMember(Name = "msdyn_solutionname")]
        internal string SolutionName { get; set; }

        [DataMember(Name = "msdyn_order")]
        internal int Order { get; set; }

        [DataMember(Name = "msdyn_name")]
        internal string Name { get; set; }
    }
}
