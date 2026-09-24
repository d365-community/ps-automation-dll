using System;
using System.Runtime.Serialization;

namespace D365.Community.Ps.Automation.Contract.Content
{
    [DataContract]
    internal enum SdkMessageProcessingStepImageImageType
    {
        [EnumMember(Value = "PreImage")]
        PreImage = 0,
        [EnumMember(Value = "PostImage")]
        PostImage = 1,
        [EnumMember(Value = "Both")]
        Both = 2
    }

    [DataContract]
    internal sealed class SdkMessageProcessingStepImage : ODataContent
    {
        [DataMember(Name = "sdkmessageprocessingstepimageid")]
        internal Guid SdkMessageProcessingStepImageId { get; set; }

        [DataMember(Name = "name")]
        internal string Name { get; set; }

        [DataMember(Name = "imagetype")]
        internal SdkMessageProcessingStepImageImageType ImageType { get; set; }

        [DataMember(Name = "attributes")]
        internal string Attributes { get; set; }

        [DataMember(Name = "entityalias")]
        internal string EntityAlias { get; set; }
    }
}