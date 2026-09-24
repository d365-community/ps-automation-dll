using System;
using System.Runtime.Serialization;

namespace D365.Community.Ps.Automation.Contract.Content
{
    [DataContract]
    internal class CallbackRegistration : ODataContent
    {
        [DataMember(Name = "callbackregistrationid")]
        internal Guid CallbackRegistrationId { get; set; }

        [DataMember(Name = "name")]
        internal string Name { get; set; }

        [DataMember(Name = "softdeletestatus")]
        internal int SoftDeleteStatus { get; set; }
    }
}
