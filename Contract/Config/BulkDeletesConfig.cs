using System.Collections.Generic;
using System.Runtime.Serialization;

namespace D365.Community.Ps.Automation.Contract.Config
{
    [DataContract]
    internal sealed class BulkDeletes : Caller
    {
        [DataMember(Name = "filter", IsRequired = false, EmitDefaultValue = false)]
        internal string Filter { get; set; } = string.Empty;

        [DataMember(Name = "bulk_deletes", IsRequired = true)]
        internal List<BulkDelete> Deletes { get; set; } = new List<BulkDelete>();
    }

    [DataContract]
    internal sealed class BulkDelete
    {
        [DataMember(Name = "name", IsRequired = true)]
        internal string Name { get; set; }

        [DataMember(Name = "recurrence_pattern", IsRequired = true)]
        internal string RecurrencePattern { get; set; }

        [DataMember(Name = "recurrence_start_time", IsRequired = true)]
        internal string RecurrenceStartTime { get; set; }

        [DataMember(Name = "disable", IsRequired = false)]
        internal bool Disable { get; set; } = false;

        //[DataMember(Name = "fetch_xml", IsRequired = false)]
        //internal string FetchXml { get; set; }

        [DataMember(Name = "query_expression", IsRequired = false)]
        internal string QueryExpression { get; set; }
    }
}
