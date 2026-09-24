using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace D365.Community.Ps.Automation.Contract.Config
{
    [DataContract]
    internal enum DataManagementAction
    {
        [EnumMember(Value = "insert")]
        Insert,
        [EnumMember(Value = "update")]
        Update,
        [EnumMember(Value = "upsert")]
        Upsert,
        [EnumMember(Value = "delete")]
        Delete,
        [EnumMember(Value = "touch")]
        Touch
    }

    [DataContract]
    internal enum DataManagementType
    {
        [EnumMember(Value = "Edm.String")]
        TypeString,
        [EnumMember(Value = "Edm.Guid")]
        TypeGuid,
        [EnumMember(Value = "Edm.Boolean")]
        TypeBoolean,
        [EnumMember(Value = "Edm.Int32")]
        TypeInt32,
        [EnumMember(Value = "Edm.Int64")]
        TypeInt64,
        [EnumMember(Value = "Edm.DateTimeOffset")]
        TypeDateTimeOffset,
        [EnumMember(Value = "Edm.Date")]
        TypeDate,
        [EnumMember(Value = "Edm.Decimal")]
        TypeDecimal,
        [EnumMember(Value = "Edm.Double")]
        TypeDouble,
        [EnumMember(Value = "Edm.Binary")]
        TypeBinary,
        [EnumMember(Value = "Edm.TimeOfDay")]
        TypeTimeOfDay,
        [EnumMember(Value = "Edm.Byte")]
        TypeByte,
        [EnumMember(Value = "Edm.Collection")]
        TypeCollection
    }

    [DataContract]
    internal sealed class DataManagementSet : Caller
    {
        [DataMember(Name = "data", IsRequired = true)]
        internal List<DataManagement> DataManagements { get; set; }
    }

    [DataContract]
    internal sealed class DataManagement
    {
        [DataMember(Name = "entity", IsRequired = true)]
        internal string Entity { get; set; }

        [DataMember(Name = "attributes", IsRequired = true)]
        internal List<KeyValue> Attributes { get; set; }

        [DataMember(Name = "action", IsRequired = true)]
        internal string ActionString
        {
            get => Enum.GetName(typeof(DataManagementAction), Action)?.ToLowerInvariant();
            set => Action = (DataManagementAction)Enum.Parse(typeof(DataManagementAction), value, true);
        }

        [IgnoreDataMember]
        internal DataManagementAction Action { get; set; }

        [DataMember(Name = "search", IsRequired = true)]
        internal Search Search { get; set; }
    }

    [DataContract]
    internal sealed class KeyValue
    {
        [DataMember(Name = "attribute", IsRequired = true)]
        internal string Attribute { get; set; }

        [DataMember(Name = "value", IsRequired = false)]
        internal object Value { get; set; }

        [DataMember(Name = "type", IsRequired = true)]
        internal string TypeString
        {
            get => Enum.GetName(typeof(DataManagementType), Type)?.Replace("Type", "Edm.");
            set => Type = (DataManagementType)Enum.Parse(typeof(DataManagementType), value.Replace("Edm.", "Type"), false);
        }

        [IgnoreDataMember]
        internal DataManagementType Type { get; set; }
    }

    [DataContract]
    internal sealed class Search
    {
        [DataMember(Name = "filter", IsRequired = false)]
        internal string Filter { get; set; }

        [DataMember(Name = "id", IsRequired = false)]
        internal Guid? Id { get; set; }

        public override string ToString()
        {
            return $"id '{Id:D}' and filter '{Filter}'";
        }
    }
}
