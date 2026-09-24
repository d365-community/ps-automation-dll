using System.Collections.Generic;
//using System.Runtime.Serialization;

namespace D365.Community.Ps.Automation.Contract.Content
{
    //[DataContract]
    internal sealed class Record : Dictionary<string, object>, IODataContent
    {
    }
}
