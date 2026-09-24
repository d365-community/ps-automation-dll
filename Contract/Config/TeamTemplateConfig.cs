using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace D365.Community.Ps.Automation.Contract.Config
{
    [DataContract]
    internal sealed class TeamTemplates : Caller
    {
        [DataMember(Name = "filter", IsRequired = false, EmitDefaultValue = false)]
        internal string Filter { get; set; } = string.Empty;

        [DataMember(Name = "team_templates", IsRequired = true)]
        internal List<TeamTemplate> Templates { get; set; } = new List<TeamTemplate>();
    }

    [DataContract]
    internal sealed class TeamTemplate
    {
        [DataMember(Name = "team_template_id", IsRequired = true)]
        internal Guid TeamTemplateId { get; set; }

        [DataMember(Name = "team_template_name", IsRequired = true)]
        internal string TeamTemplateName { get; set; }

        [DataMember(Name = "entity", IsRequired = true)]
        internal string Entity { get; set; }

        //[DataMember(Name = "object_type_code")]
        //internal int ObjectTypeCode { get; set; }

        [DataMember(Name = "description", IsRequired = false, EmitDefaultValue = false)]
        internal string Description { get; set; }

        [DataMember(Name = "default_access_rights_mask", IsRequired = false, EmitDefaultValue = false)]
        internal int DefaultAccessRightsMask { get; set; }

        //[DataMember(Name = "is_system", IsRequired = false, EmitDefaultValue = false)]
        //internal bool? IsSystem { get; set; }
    }
}
