using System;
using System.Runtime.Serialization;

namespace D365.Community.Ps.Automation.Contract.Content
{
    [DataContract]
    internal sealed class SdkMessageProcessingStepSecureConfig : ODataContent
    {
        [DataMember(Name = "sdkmessageprocessingstepsecureconfigid")]
        internal Guid SdkMessageProcessingStepSecureConfigId { get; set; }

        [DataMember(Name = "name")]
        internal string Name { get; set; }

        [DataMember(Name = "secureconfig")]
        internal string SecureConfig { get; set; }
    }
}
