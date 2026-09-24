using System.Runtime.Serialization;

namespace D365.Community.Ps.Automation.Contract.Config
{
    [DataContract]
    internal sealed class ConnectionReferencesConfig : Caller
    {
        [DataMember(Name = "filter", IsRequired = false, EmitDefaultValue = false)]
        internal string Filter { get; set; }
    }
}
