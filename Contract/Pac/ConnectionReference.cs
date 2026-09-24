using System.Runtime.Serialization;

namespace D365.Community.Ps.Automation.Contract.Pac
{
    [DataContract]
    internal sealed class ConnectionReference
    {
        [DataMember(Name = "LogicalName")]
        internal string LogicalName { get; set; }

        [DataMember(Name = "ConnectionId")]
        internal string ConnectionId { get; set; }

        [DataMember(Name = "ConnectorId")]
        internal string ConnectorId { get; set; }
    }
}
