using System;
using System.Runtime.Serialization;

namespace D365.Community.Ps.Automation.Contract.Content
{
    [DataContract]
    internal sealed class WebResource : ODataContent
    {
        [DataMember(Name = "webresourceid")]
        internal Guid WebResourceId { get; set; }

        [DataMember(Name = "name")]
        internal string Name { get; set; }

        [DataMember(Name = "content")]
        internal string Content { get; set; }

        [DataMember(Name = "languagecode")]
        internal int? LanguageCode { get; set; }
    }
}
