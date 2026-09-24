using System.Runtime.Serialization;

namespace D365.Community.Ps.Automation.Contract.Content
{
    [DataContract]
    internal class ODataContent : IODataContent
    {
        [DataMember(Name = "@odata.context", IsRequired = false, EmitDefaultValue = false)]
        internal string Context { get; set; }

        [DataMember(Name = "@odata.etag", IsRequired = false, EmitDefaultValue = false)]
        internal string Etag { get; set; }
    }
}
