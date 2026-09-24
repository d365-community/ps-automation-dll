using System;
using System.Runtime.Serialization;

namespace D365.Community.Ps.Automation.Contract.Content
{
    [DataContract]
    internal sealed class DocumentTemplate : ODataContent
    {
        [DataMember(Name = "documenttemplateid")]
        internal Guid DocumentTemplateId { get; set; }

        [DataMember(Name = "name")]
        internal string Name { get; set; }

        [DataMember(Name = "description")]
        internal string Description { get; set; }

        [DataMember(Name = "associatedentitytypecode")]
        internal string AssociatedEntityTypeCode { get; set; }

        [DataMember(Name = "documenttype")]
        internal int DocumentType { get; set; }

        [DataMember(Name = "status")]
        internal bool Status { get; set; }

        [DataMember(Name = "languagecode")]
        internal int LanguageCode { get; set; }

        [DataMember(Name = "clientdata")]
        internal string ClientData { get; set; }

        [DataMember(Name = "content")]
        internal string Content { get; set; }
    }
}
