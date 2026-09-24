using System.Runtime.Serialization;

namespace D365.Community.Ps.Automation.Contract.Content
{
    [DataContract]
    internal sealed class ErrorResponse
    {
        [DataMember(Name = "code")]
        internal string Code { get; set; }

        [DataMember(Name = "message")]
        internal string Message { get; set; }
    }
}
