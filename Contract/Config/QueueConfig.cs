using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace D365.Community.Ps.Automation.Contract.Config
{
    [DataContract]
    internal sealed class Queues : Caller
    {
        [DataMember(Name = "filter", IsRequired = false, EmitDefaultValue = false)]
        internal string Filter { get; set; } = string.Empty;

        [DataMember(Name = "queues", IsRequired = false)]
        internal List<Queue> ListOfQueues { get; set; } = new List<Queue>();
    }

    [DataContract]
    internal sealed class Queue
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
