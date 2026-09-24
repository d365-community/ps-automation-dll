using System;
using System.Runtime.Serialization;

namespace D365.Community.Ps.Automation.Contract.Content
{
    [DataContract]
    internal sealed class EnvironmentVariableValue : ODataContent
    {
        [DataMember(Name = "environmentvariablevalueid")]
        internal Guid EnvironmentVariableValueId { get; set; }

        [DataMember(Name = "value")]
        internal string Value { get; set; }
    }
}
