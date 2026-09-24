using System;
using System.Runtime.Serialization;

namespace D365.Community.Ps.Automation.Contract.Content
{
    [DataContract]
    internal sealed class AsyncOperation : ODataContent
    {
        [DataMember(Name = "asyncoperationid")]
        internal Guid AsyncOperationId { get; set; }

        [DataMember(Name = "name")]
        internal string Name { get; set; }

        [DataMember(Name = "friendlymessage")]
        internal string FriendlyMessage { get; set; }

        [DataMember(Name = "message")]
        internal string Message { get; set; }

        [DataMember(Name = "statuscode")]
        internal int? StatusCode { get; set; }

        [DataMember(Name = "data")]
        internal string Data { get; set; }

        [DataMember(Name = "recurrencepattern")]
        internal string RecurrencePattern { get; set; }

        [DataMember(Name = "recurrencestarttime")]
        internal DateTime? RecurrenceStartTime { get; set; }
    }
}
