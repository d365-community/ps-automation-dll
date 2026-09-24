using System.Runtime.Serialization;

namespace D365.Community.Ps.Automation.Contract.Pac
{
    [DataContract]
    internal sealed class EnvironmentVariable
    {
        [DataMember(Name = "SchemaName")]
        internal string SchemaName { get; set; }

        [DataMember(Name = "Value")]
        internal string Value { get; set; }
    }
}
