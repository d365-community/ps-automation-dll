using System;
using System.Runtime.Serialization;

namespace D365.Community.Ps.Automation.Contract.Content
{
    [DataContract]
    internal sealed class ConnectionReference : ODataContent
    {
        [DataMember(Name = "connectionreferenceid")]
        internal Guid ConnectionReferenceId { get; set; }

        [DataMember(Name = "connectionreferencedisplayname")]
        internal string ConnectionReferenceDisplayName { get; set; }

        [DataMember(Name = "connectionreferencelogicalname")]
        internal string ConnectionReferenceLogicalName { get; set; }

        [DataMember(Name = "connectorid")]
        internal string ConnectorId { get; set; }
    }
}
