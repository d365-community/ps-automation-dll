using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace D365.Community.Ps.Automation.Contract.Content
{
    [DataContract]
    internal class Workflow : ODataContent
    {
        [DataMember(Name = "workflowid")]
        internal Guid WorkflowId { get; set; }

        [DataMember(Name = "category")]
        internal int Category { get; set; }

        [DataMember(Name = "name")]
        internal string Name { get; set; }

        [DataMember(Name = "uniquename")]
        internal string UniqueName { get; set; }

        [DataMember(Name = "primaryentity")]
        internal string PrimaryEntity { get; set; }

        [DataMember(Name = "clientdata", IsRequired = false, EmitDefaultValue = false)]
        internal string ClientData { get; set; }

        [DataMember(Name = "_ownerid_value")]
        internal Guid OwnerId { get; set; }

        [DataMember(Name = "_owninguser_value")]
        internal Guid? OwningUser { get; set; }

        [DataMember(Name = "_owningteam_value")]
        internal Guid? OwningTeam { get; set; }

        [DataMember(Name = "statecode")]
        internal int StateCode { get; set; }

        [DataMember(Name = "statuscode")]
        internal int StatusCode { get; set; }
    }

    [DataContract]
    internal class ClientData
    {
        [DataMember(Name = "properties")]
        internal Properties Properties { get; set; }
    }

    [DataContract]
    internal class Properties
    {
        [DataMember(Name = "definition")]
        internal Definition Definition { get; set; }
    }

    [DataContract]
    internal class Definition
    {
        [DataMember(Name = "triggers")]
        internal Dictionary<string, Trigger> Triggers { get; set; }
    }

    [DataContract]
    internal class Trigger
    {
        [DataMember(Name = "type")]
        internal string Type { get; set; }
    }
}
