using System.Runtime.Serialization;

namespace D365.Community.Ps.Automation.Contract.Content
{
    [DataContract]
    internal sealed class EntityUserLocalizedLabelMetadata
    {
        [DataMember(Name = "Label")]
        internal string Label { get; set; }
    }
}
