using System.Runtime.Serialization;

namespace D365.Community.Ps.Automation.Contract.Content
{
    [DataContract]
    internal sealed class EntityDisplayNameMetadata
    {
        [DataMember(Name = "UserLocalizedLabel")]
        internal EntityUserLocalizedLabelMetadata UserLocalizedLabel { get; set; }
    }
}
