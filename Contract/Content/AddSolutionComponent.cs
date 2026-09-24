using System;
using System.Runtime.Serialization;

namespace D365.Community.Ps.Automation.Contract.Content
{
    [DataContract]
    internal sealed class AddSolutionComponent : IODataContent
    {
        [DataMember(Name = "@odata.context")]
        internal string Context { get; set; }

        [DataMember(Name = "id")]
        internal Guid Id { get; set; }
    }
}
