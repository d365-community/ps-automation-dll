using System.Collections;
using System.Runtime.Serialization;

namespace D365.Community.Ps.Automation.Contract.Content
{
    [DataContract]
    internal class ODataContents<T> : IODataContents where T : IList
    {
        [DataMember(Name = "@odata.context", IsRequired = true)]
        internal string Context { get; set; }

        [DataMember(Name = "@odata.nextLink", IsRequired = false, EmitDefaultValue = false)]
        internal string NextLink { get; set; }

        [DataMember(Name = "value", IsRequired = true)]
        internal T Value { get; set; }

        string IODataContents.NextLink()
        {
            return NextLink;
        }

        void IODataContents.AddRange(IList range)
        {
            foreach (var any in range)
            {
                Value.Add(any);
            }
        }

        IList IODataContents.GetList()
        {
            return Value;
        }

        int IODataContents.Count()
        {
            return Value.Count;
        }
    }
}
