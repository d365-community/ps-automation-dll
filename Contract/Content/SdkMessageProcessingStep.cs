using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace D365.Community.Ps.Automation.Contract.Content
{
    [DataContract]
    internal enum SdkMessageProcessingStepStage
    {
        [EnumMember]
        PreValidation = 10,
        [EnumMember]
        PreOperation = 20,
        [EnumMember]
        MainOperation = 30,
        [EnumMember]
        PostOperation = 40
    }

    [DataContract]
    internal enum SdkMessageProcessingStepMode
    {
        [EnumMember]
        Asynchronous = 1,
        [EnumMember]
        Synchronous = 0
    }

    [DataContract]
    internal sealed class SdkMessageProcessingStep : ODataContent
    {
        [DataMember(Name = "sdkmessageprocessingstepid")]
        internal Guid SdkMessageProcessingStepId { get; set; }

        [DataMember(Name = "name")]
        internal string Name { get; set; }

        [DataMember(Name = "description")]
        internal string Description { get; set; }

        [DataMember(Name = "rank")]
        internal int Rank { get; set; }

        [DataMember(Name = "filteringattributes")]
        internal string FilteringAttributes { get; set; }

        [DataMember(Name = "asyncautodelete")]
        internal bool AsyncAutoDelete { get; set; }

        [DataMember(Name = "mode")]
        internal SdkMessageProcessingStepMode Mode { get; set; }

        [DataMember(Name = "stage")]
        internal SdkMessageProcessingStepStage Stage { get; set; }

        [DataMember(Name = "configuration")]
        internal string Configuration { get; set; }

        [DataMember(Name = "_sdkmessageprocessingstepsecureconfigid_value")]
        internal Guid? SdkMessageProcessingStepSecureConfigId { get; set; }

        [DataMember(Name = "sdkmessageprocessingstepid_sdkmessageprocessingstepimage")]
        internal List<SdkMessageProcessingStepImage> SdkMessageProcessingStepImages { get; set; } = new List<SdkMessageProcessingStepImage>();

        //TODO: nice to have
        [DataMember(Name = "impersonatinguserid")]
        internal Guid ImpersonatingUserId { get; set; }
    }
}