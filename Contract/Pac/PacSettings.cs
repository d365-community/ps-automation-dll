using System.Collections.Generic;
using System.Runtime.Serialization;

namespace D365.Community.Ps.Automation.Contract.Pac
{
    //https://learn.microsoft.com/en-us/power-platform/alm/conn-ref-env-variables-build-tools

    [DataContract]
    internal sealed class PacSettings
    {
        [DataMember(Name = "EnvironmentVariables", IsRequired = false, EmitDefaultValue = false)]
        internal List<EnvironmentVariable> EnvironmentVariables { get; set; } = new List<EnvironmentVariable>();

        [DataMember(Name = "ConnectionReferences", IsRequired = false, EmitDefaultValue = false)]
        internal List<ConnectionReference> ConnectionReferences { get; set; } = new List<ConnectionReference>();
    }
}
