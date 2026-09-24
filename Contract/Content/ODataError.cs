using System.Runtime.Serialization;

namespace D365.Community.Ps.Automation.Contract.Content
{
    [DataContract]
    internal sealed class ODataError : IODataContent, IODataError
    {
        [DataMember(Name = "error")]
        internal ErrorResponse Error { get; set; }

        public string GetErrorMessage() => Error.Message;
    }
}
