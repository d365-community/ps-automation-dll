using System;
using System.Runtime.Serialization;

namespace D365.Community.Ps.Automation.Contract.Content
{
    [DataContract]
    internal sealed class TeamTemplate : ODataContent
    {
        [DataMember(Name = "teamtemplateid")]
        internal Guid TeamTemplateId { get; set; }

        [DataMember(Name = "teamtemplatename")]
        internal string TeamTemplateName { get; set; }

        [DataMember(Name = "objecttypecode")]
        internal int ObjectTypeCode { get; set; }

        [DataMember(Name = "description")]
        internal string Description { get; set; }

        [DataMember(Name = "defaultaccessrightsmask")]
        internal int DefaultAccessRightsMask { get; set; }

        //[DataMember(Name = "issystem")]
        //internal bool IsSystem { get; set; }
    }
}
