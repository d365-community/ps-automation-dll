using System.Runtime.Serialization;

namespace D365.Community.Ps.Automation.Contract.Assembly
{
    [DataContract]
    internal sealed class SdkMessageProcessingStepImage
    {
        [DataMember(Name = "Image", IsRequired = true)]
        internal string Image { get; set; }

        [DataMember(Name = "ImageType", IsRequired = true)]
        internal string ImageType { get; set; }

        [DataMember(Name = "EntityAlias", IsRequired = false, EmitDefaultValue = false)]
        internal string EntityAlias { get; set; }

        [DataMember(Name = "Parameters", IsRequired = false, EmitDefaultValue = false)]
        internal string Parameters { get; set; }
    }
}