using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace D365.Community.Ps.Automation.Contract.Config
{
    [DataContract]
    internal enum SolutionComponentsAction
    {
        [EnumMember(Value = "include")]
        Include,
        [EnumMember(Value = "exclude")]
        Exclude
    }

    [DataContract]
    internal sealed class SolutionsComponents : Caller
    {
        [DataMember(Name = "solutions", IsRequired = false)]
        internal List<SolutionComponents> ListSolutionComponents { get; set; } = new List<SolutionComponents>();
    }

    [DataContract]
    internal sealed class SolutionComponents
    {
        [DataMember(Name = "solution", IsRequired = true)]
        internal string Solution { get; set; }

        [DataMember(Name = "strict", IsRequired = true)]
        internal bool Strict { get; set; }

        [DataMember(Name = "types", IsRequired = true)]
        internal List<int> Types { get; set; }

        [DataMember(Name = "action")]
        internal string ActionString
        {
            get => Enum.GetName(typeof(SolutionComponentsAction), Action)?.ToLowerInvariant();
            set => Action = (SolutionComponentsAction)Enum.Parse(typeof(SolutionComponentsAction), value, true);
        }

        [IgnoreDataMember]
        internal SolutionComponentsAction Action { get; set; }
    }
}
