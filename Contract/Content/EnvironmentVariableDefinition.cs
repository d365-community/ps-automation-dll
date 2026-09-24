using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace D365.Community.Ps.Automation.Contract.Content
{
    [DataContract]
    internal sealed class EnvironmentVariableDefinition : ODataContent
    {
        [DataMember(Name = "environmentvariabledefinitionid")]
        internal Guid EnvironmentVariableDefinitionId { get; set; }

        [DataMember(Name = "defaultvalue")]
        internal string DefaultValue { get; set; }

        [DataMember(Name = "description")]
        internal string Description { get; set; }

        [DataMember(Name = "schemaname")]
        internal string SchemaName { get; set; }

        [DataMember(Name = "environmentvariabledefinition_environmentvariablevalue")]
        internal List<EnvironmentVariableValue> EnvironmentVariableValue { get; set; } = new List<EnvironmentVariableValue>();
    }
}
