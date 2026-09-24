using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace D365.Community.Ps.Automation.Contract.Config
{
    [DataContract]
    internal sealed class DocumentTemplates : Caller
    {
        [DataMember(Name = "ignore_missing", IsRequired = false)]
        internal bool IgnoreMissing { get; set; } = true;

        [DataMember(Name = "filter", IsRequired = false, EmitDefaultValue = false)]
        internal string Filter { get; set; } = string.Empty;

        [DataMember(Name = "file_dir", IsRequired = false)]
        internal string FileDir { get; set; } = "document_template";

        [DataMember(Name = "documenttemplates", IsRequired = true)]
        internal List<DocumentTemplate> Templates { get; set; } = new List<DocumentTemplate>();
    }

    [DataContract]
    internal sealed class DocumentTemplate
    {
        [DataMember(Name = "documenttemplateid", IsRequired = false, EmitDefaultValue = false)]
        internal Guid? DocumentTemplateId { get; set; }

        [DataMember(Name = "name", IsRequired = true)]
        internal string Name { get; set; }

        //MicrosoftExcel = 1,MicrosoftWord = 2
        [DataMember(Name = "document_type", IsRequired = true)]
        internal int DocumentType { get; set; }

        //Draft = 1,Activated = 0
        [DataMember(Name = "status", IsRequired = true)]
        internal bool Status { get; set; }

        [DataMember(Name = "force_update", IsRequired = false)]
        internal bool ForceUpdate { get; set; } = false;

        [DataMember(Name = "file", IsRequired = false, EmitDefaultValue = false)]
        internal string File { get; set; }

        [DataMember(Name = "language_code", IsRequired = true)]
        internal int LanguageCode { get; set; }

        [DataMember(Name = "description", IsRequired = false, EmitDefaultValue = false)]
        internal string Description { get; set; }

        [DataMember(Name = "associated_entity", IsRequired = true)]
        internal string AssociatedEntityTypeCode { get; set; }

        //[DataMember(Name = "client_data", IsRequired = false, EmitDefaultValue = false)]
        //internal string ClientData { get; set; }
    }
}
