using System;
using System.Runtime.Serialization;

namespace D365.Community.Ps.Automation.Contract.Content
{
    [DataContract]
    internal sealed class SavedQuery : ODataContent
    {
        [DataMember(Name = "savedqueryid")]
        internal Guid SavedQueryId { get; set; }

        [DataMember(Name = "name")]
        internal string Name { get; set; }

        [DataMember(Name = "description")]
        internal string Description { get; set; }

        [DataMember(Name = "fetchxml")]
        internal string FetchXml { get; set; }

        [DataMember(Name = "isdefault")]
        internal bool IsDefault { get; set; }

        [DataMember(Name = "returnedtypecode")]
        internal string ReturnedTypeCode { get; set; }

        [DataMember(Name = "statecode")]
        internal int StateCode { get; set; }

        [DataMember(Name = "statuscode")]
        internal int StatusCode { get; set; }
    }
}
