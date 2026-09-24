using System;
using System.Runtime.Serialization;

namespace D365.Community.Ps.Automation.Contract.Content
{
    [DataContract]
    internal sealed class Queue : ODataContent
    {
        [DataMember(Name = "queueid")]
        internal Guid QueueId { get; set; }

        [DataMember(Name = "name")]
        internal string Name { get; set; }

        [DataMember(Name = "description")]
        internal string Description { get; set; }

        [DataMember(Name = "queueviewtype")]
        internal int QueueViewType { get; set; }

        [DataMember(Name = "outgoingemaildeliverymethod")]
        internal int OutgoingEmailDeliveryMethod { get; set; }

        [DataMember(Name = "incomingemaildeliverymethod")]
        internal int IncomingEmailDeliveryMethod { get; set; }

        [DataMember(Name = "incomingemailfilteringmethod")]
        internal int IncomingEmailFilteringMethod { get; set; }
    }
}
