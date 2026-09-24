using System.Collections.Generic;
using System.Runtime.Serialization;

namespace D365.Community.Ps.Automation.Contract.Content
{
    [DataContract]
    internal sealed class QueryExpression : IODataContent
    {
        [DataMember(Name = "@odata.context")]
        internal string Context { get; set; }

        [DataMember(Name = "Query")]
        internal Query Query { get; set; }
    }

    //https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/reference/queryexpression?view=dataverse-latest
    [DataContract]
    internal sealed class Query
    {
        [DataMember(Name = "ColumnSet")]
        internal ColumnSet ColumnSet { get; set; }

        [DataMember(Name = "Criteria")]
        internal FilterExpression Criteria { get; set; }

        [DataMember(Name = "DataSource", EmitDefaultValue = false)]
        internal string DataSource { get; set; }

        [DataMember(Name = "Distinct")]
        internal bool Distinct { get; set; }

        [DataMember(Name = "EntityName")]
        internal string EntityName { get; set; }

        [DataMember(Name = "ForceSeek", EmitDefaultValue = false)]
        internal string ForceSeek { get; set; }
        
        [DataMember(Name = "LinkEntities")]
        internal List<LinkEntity> LinkEntities { get; set; } = new List<LinkEntity>();

        [DataMember(Name = "NoLock")]
        internal bool NoLock { get; set; }

        [DataMember(Name = "Orders")]
        internal List<OrderExpression> Orders { get; set; } = new List<OrderExpression>();

        [DataMember(Name = "PageInfo")]
        internal PagingInfo PageInfo { get; set; }

        [DataMember(Name = "QueryHints", EmitDefaultValue = false)]
        internal string QueryHints { get; set; }

        [DataMember(Name = "SubQueryExpression", EmitDefaultValue = false)]
        internal Query SubQueryExpression { get; set; }

        [DataMember(Name = "TopCount", EmitDefaultValue = false)]
        internal int? TopCount { get; set; }
    }

    //https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/reference/columnset?view=dataverse-latest
    [DataContract]
    internal sealed class ColumnSet
    {
        [DataMember(Name = "AllColumns")]
        internal bool AllColumns { get; set; }

        [DataMember(Name = "AttributeExpressions")]
        internal List<AttributeExpression> AttributeExpressions { get; set; } = new List<AttributeExpression>();

        [DataMember(Name = "Columns")]
        internal List<string> Columns { get; set; } = new List<string>();
    }

    //https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/reference/xrmattributeexpression?view=dataverse-latest
    [DataContract]
    internal sealed class AttributeExpression
    {
        //is enum, but...
        [DataMember(Name = "AggregateType", EmitDefaultValue = false)]
        internal string AggregateType { get; set; }

        [DataMember(Name = "Alias")]
        internal string Alias { get; set; }

        [DataMember(Name = "AttributeName")]
        internal string AttributeName { get; set; }

        //is enum, but...
        [DataMember(Name = "DateTimeGrouping", EmitDefaultValue = false)]
        internal string DateTimeGrouping { get; set; }

        [DataMember(Name = "HasGroupBy")]
        internal bool HasGroupBy { get; set; }
    }

    //https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/reference/filterexpression?view=dataverse-latest
    [DataContract]
    internal sealed class FilterExpression
    {
        [DataMember(Name = "AnyAllFilterLinkEntity", EmitDefaultValue = false)]
        internal LinkEntity AnyAllFilterLinkEntity { get; set; }

        [DataMember(Name = "Conditions")]
        internal List<ConditionExpression> Conditions { get; set; } = new List<ConditionExpression>();

        [DataMember(Name = "FilterHint", EmitDefaultValue = false)]
        internal string FilterHint { get; set; }

        //is enum, but...
        [DataMember(Name = "FilterOperator")]
        internal string FilterOperator { get; set; } = "And";

        [DataMember(Name = "Filters")]
        internal List<FilterExpression> Filters { get; set; } = new List<FilterExpression>();

        [DataMember(Name = "IsQuickFindFilter")]
        internal bool IsQuickFindFilter { get; set; }
    }

    //https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/reference/conditionexpression?view=dataverse-latest
    [DataContract]
    internal sealed class ConditionExpression
    {
        [DataMember(Name = "AttributeName")]
        internal string AttributeName { get; set; }

        [DataMember(Name = "CompareColumns")]
        internal bool CompareColumns { get; set; }

        [DataMember(Name = "EntityName")]
        internal string EntityName { get; set; }

        //is enum, but...
        [DataMember(Name = "Operator")]
        internal string Operator { get; set; }

        [DataMember(Name = "Values")]
        internal List<Object> Values { get; set; } = new List<Object>();
    }

    //https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/reference/object?view=dataverse-latest
    [DataContract]
    internal sealed class Object
    {
        private string _value;

        [DataMember(Name = "Type")]
        internal string Type { get; set; }

        //MS; nasty exception -> inconsistent results.
        [DataMember(Name = "Value")]
        internal object ValueObject
        {
            get => _value;
            set => _value = (value == null) ? "" : value.ToString();
        }
    }

    //https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/reference/linkentity?view=dataverse-latest
    [DataContract]
    internal sealed class LinkEntity
    {
        [DataMember(Name = "Columns")]
        internal ColumnSet Columns { get; set; }

        [DataMember(Name = "EntityAlias", EmitDefaultValue = false)]
        internal string EntityAlias { get; set; }

        [DataMember(Name = "ForceSeek", EmitDefaultValue = false)]
        internal string ForceSeek { get; set; }

        //is enum, but...
        [DataMember(Name = "JoinOperator")]
        internal string JoinOperator { get; set; }

        //is enum, but...
        [DataMember(Name = "LinkCriteria", EmitDefaultValue = false)]
        internal FilterExpression LinkCriteria { get; set; }

        [DataMember(Name = "LinkEntities")]
        internal List<LinkEntity> LinkEntities { get; set; } = new List<LinkEntity>();

        [DataMember(Name = "LinkFromAttributeName", EmitDefaultValue = false)]
        internal string LinkFromAttributeName { get; set; }

        [DataMember(Name = "LinkFromEntityName", EmitDefaultValue = false)]
        internal string LinkFromEntityName { get; set; }

        [DataMember(Name = "LinkToAttributeName", EmitDefaultValue = false)]
        internal string LinkToAttributeName { get; set; }

        [DataMember(Name = "LinkToEntityName", EmitDefaultValue = false)]
        internal string LinkToEntityName { get; set; }

        [DataMember(Name = "Orders")]
        internal List<OrderExpression> Orders { get; set; } = new List<OrderExpression>();
    }

    //https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/reference/orderexpression?view=dataverse-latest
    [DataContract]
    internal sealed class OrderExpression
    {
        [DataMember(Name = "Alias", EmitDefaultValue = false)]
        internal string Alias { get; set; }

        [DataMember(Name = "AttributeName")]
        internal string AttributeName { get; set; }

        [DataMember(Name = "EntityName")]
        internal string EntityName { get; set; }

        //is enum, but...
        [DataMember(Name = "OrderType")]
        internal string OrderType { get; set; }
    }

    //https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/reference/paginginfo?view=dataverse-latest
    [DataContract]
    internal sealed class PagingInfo
    {
        [DataMember(Name = "Count")]
        internal int Count { get; set; }

        [DataMember(Name = "PageNumber")]
        internal int PageNumber { get; set; }

        [DataMember(Name = "PagingCookie")]
        internal string PagingCookie { get; set; }

        [DataMember(Name = "ReturnTotalRecordCount")]
        internal bool ReturnTotalRecordCount { get; set; }
    }
}
