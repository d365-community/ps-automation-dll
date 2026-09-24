using System.Runtime.Serialization;

namespace D365.Community.Ps.Automation.Contract.Content
{
    [DataContract]
    internal sealed class InitializeFileBlocksDownload : ODataContent
    {
        [DataMember(Name = "FileSizeInBytes")]
        internal long FileSizeInBytes { get; set; }

        [DataMember(Name = "FileName")]
        internal string FileName { get; set; }

        [DataMember(Name = "FileContinuationToken")]
        internal string FileContinuationToken { get; set; }

        [DataMember(Name = "IsChunkingSupported")]
        internal bool IsChunkingSupported { get; set; }
    }
}
