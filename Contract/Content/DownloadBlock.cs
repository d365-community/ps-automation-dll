using System;
using System.Runtime.Serialization;

namespace D365.Community.Ps.Automation.Contract.Content
{
    [DataContract]
    internal sealed class DownloadBlock : ODataContent
    {
        [DataMember(Name = "Data")]
        internal string Data { get; set; }

        internal byte[] GetBytes()
        {
            return Convert.FromBase64String(Data);
        }
    }
}
