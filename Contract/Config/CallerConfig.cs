using System.Runtime.Serialization;

namespace D365.Community.Ps.Automation.Contract.Config
{
    [DataContract]
    internal class Caller
    {
        [DataMember(Name = "CallerObjectId", IsRequired = false, EmitDefaultValue = false)]
        internal string CallerObjectId { get; set; }

        [DataMember(Name = "MSCRMCallerID", IsRequired = false, EmitDefaultValue = false)]
        internal string MsCrmCallerId { get; set; }
    }
}
