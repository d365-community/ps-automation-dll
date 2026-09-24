using D365.Community.Ps.Automation.Contract.Config;
using D365.Community.Ps.Automation.Contract.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using D365.Community.Ps.Automation.Service;

namespace D365.Community.Ps.Automation.Job
{
    internal static class DataManagementJob
    {
        internal static bool Execute(DataManagementSet config)
        {
            var result = true;
            foreach (var data in config.DataManagements)
            {
                if (!GetEntity(data.Entity, out var entity))
                {
                    result = false;
                    continue;
                }
                if (GetRecord(entity.LogicalCollectionName, data.Attributes, data.Search, out var record))
                {
                    if (data.Action == DataManagementAction.Insert && record == null)
                    {
                        result = CreateRecord(entity.LogicalCollectionName, data.Entity, data.Search.Id, data.Attributes) && result;
                    }
                    else if (data.Action == DataManagementAction.Insert && record != null)
                    {
                        Console.WriteLine($"Information: record '{record[$"{data.Entity}id"]}' exists ...");
                    }
                    else if (data.Action == DataManagementAction.Update && record == null)
                    {
                        Console.Error.WriteLine($"ERROR: action '{data.Action}' plus search {data.Search} invalid!");
                        result = false;
                    }
                    else if (data.Action == DataManagementAction.Update && record != null)
                    {
                        result = UpdateRecord(entity.LogicalCollectionName, data.Entity, record, data.Attributes) && result;
                    }
                    else if (data.Action == DataManagementAction.Upsert && record == null)
                    {
                        result = CreateRecord(entity.LogicalCollectionName, data.Entity, data.Search.Id, data.Attributes) && result;
                    }
                    else if (data.Action == DataManagementAction.Upsert && record != null)
                    {
                        result = UpdateRecord(entity.LogicalCollectionName, data.Entity, record, data.Attributes) && result;
                    }
                    else if (data.Action == DataManagementAction.Delete && record == null)
                    {
                        Console.WriteLine("Information: record does not exist ...");
                    }
                    else if (data.Action == DataManagementAction.Delete && record != null)
                    {
                        result = DeleteRecord(entity.LogicalCollectionName, Guid.Parse((string)record[$"{data.Entity}id"])) && result;
                    }
                    else if (data.Action == DataManagementAction.Touch && record != null)
                    {
                        var attributes = new List<KeyValue>();
                        foreach (var keyValue in data.Attributes.Where(keyValue => record.ContainsKey(keyValue.Attribute)))
                        {
                            record[keyValue.Attribute] = keyValue.Value;
                        }
                        result = UpdateRecord(entity.LogicalCollectionName, data.Entity, record, attributes, true) && result;
                    }
                    else if (data.Action == DataManagementAction.Touch && record == null)
                    {
                        Console.Error.WriteLine($"ERROR: action '{data.Action}' plus search {data.Search} invalid!");
                        result = false;
                    }
                }
                else
                {
                    result = false;
                }
            }
            return result;
        }

        private static bool GetEntity(string logicalname, out Entity entity)
        {
            entity = null;
            var entities = $"{PsAutomation.ApiUrl}/entities?$select=name,logicalname,originallocalizedname,logicalcollectionname&$filter={Uri.EscapeDataString($"logicalname eq '{logicalname}'")}";
            var result = Client.Fetch<ODataContents<List<Entity>>>(entities, out var response);
            if (response.StatusCode == 200)
            {
                var content = (ODataContents<List<Entity>>)response.Content;
                if (content.Value == null || content.Value.Count < 1)
                {
                    Console.Error.WriteLine($"ERROR: entity '{logicalname}' not found!");
                    return false;//not found
                }
                entity = content.Value.First();
            }
            return result;
        }

        private static bool CreateRecord(string entities, string entity, Guid? id, List<KeyValue> attributes)
        {
            Console.WriteLine($"Create record in '{entity}' for attributes '{string.Join(",", attributes.Select(e => e.Attribute))}'");

            var url = $"{PsAutomation.ApiUrl}/{entities}";
            var body = new StringBuilder().OpenJson();
            if ((id != null && id != Guid.Empty))
            {
                body.AppendValue($"{entity}id", id);
            }
            foreach (var keyValue in attributes)
            {
                var attribute = keyValue.Attribute;
                var value = keyValue.Value;
                var type = keyValue.Type;
                if (value == null)
                {
                    body.AppendNull(attribute);
                }
                else
                {
                    switch (type)
                    {
                        case DataManagementType.TypeString:
                            body.AppendValue(attribute, (string)value);
                            break;
                        case DataManagementType.TypeGuid:
                            body.AppendValue(attribute, (Guid)value);
                            break;
                        case DataManagementType.TypeBoolean:
                            body.AppendValue(attribute, (bool)value);
                            break;
                        case DataManagementType.TypeInt32:
                            body.AppendValue(attribute, (int)value);
                            break;
                        case DataManagementType.TypeInt64:
                            body.AppendValue(attribute, (long)value);
                            break;
                        case DataManagementType.TypeCollection:
                            body.AppendValue(attribute, (List<object>)value);
                            break;
                        default:
                            Console.WriteLine($"WARNING: treating '{type}' for attribute '{attribute}' as value.ToString() --> final implementation missing so far. Therefore it's experimental and may not work ...");
                            body.AppendObject(attribute, value.ToString());
                            break;
                    }
                }
            }
            body.CloseJson();
            return Client.Post(url, body.ToString(), out _);
        }

        private static bool UpdateRecord(string entities, string entity, Record record, List<KeyValue> attributes, bool touch = false)
        {
            var updates = new List<string>();
            var id = Guid.Parse((string)record[$"{entity}id"]);
            var url = $"{PsAutomation.ApiUrl}/{entities}({id:D})";
            var body = new StringBuilder().OpenJson();
            foreach (var keyValue in attributes)
            {
                var attribute = keyValue.Attribute;
                Console.WriteLine($"Check attribute '{attribute}':");
                var value = keyValue.Value;
                var type = keyValue.Type;
                if (value == null)
                {
                    Console.WriteLine(" - Clear ...");
                    updates.Add(attribute);
                    body.AppendNull(attribute);
                }
                else
                {
                    switch (type)
                    {
                        case DataManagementType.TypeString:
                            {
                                if (!touch && record.TryGetValue(attribute, out var existing))
                                {
                                    if (existing == null || (string)existing != (string)value)
                                    {
                                        Console.WriteLine(" - Update ...");
                                        updates.Add(attribute);
                                        body.AppendValue(attribute, (string)value);
                                    }
                                }
                                else
                                {
                                    Console.WriteLine(" - Touch ...");
                                    updates.Add(attribute);
                                    body.AppendValue(attribute, (string)value);
                                }
                            }
                            break;
                        case DataManagementType.TypeGuid:
                            {
                                if (!touch && record.TryGetValue(attribute, out var existing))
                                {
                                    if (existing == null || Guid.Parse((string)existing) != (Guid)value)
                                    {
                                        Console.WriteLine(" - Update ...");
                                        updates.Add(attribute);
                                        body.AppendValue(attribute, (Guid?)value);
                                    }
                                }
                                else
                                {
                                    Console.WriteLine(" - Touch ...");
                                    updates.Add(attribute);
                                    body.AppendValue(attribute, (Guid?)value);
                                }
                            }
                            break;
                        case DataManagementType.TypeBoolean:
                            {
                                if (!touch && record.TryGetValue(attribute, out var existing))
                                {
                                    if (existing == null || (bool)existing != (bool)value)
                                    {
                                        Console.WriteLine(" - Update ...");
                                        updates.Add(attribute);
                                        body.AppendValue(attribute, (bool)value);
                                    }
                                }
                                else
                                {
                                    Console.WriteLine(" - Touch ...");
                                    updates.Add(attribute);
                                    body.AppendValue(attribute, (bool)value);
                                }
                            }
                            break;
                        case DataManagementType.TypeInt32:
                            {
                                if (!touch && record.TryGetValue(attribute, out var existing))
                                {
                                    if (existing == null || (int)existing != (int)value)
                                    {
                                        Console.WriteLine(" - Update ...");
                                        updates.Add(attribute);
                                        body.AppendValue(attribute, (int)value);
                                    }
                                }
                                else
                                {
                                    Console.WriteLine(" - Touch ...");
                                    updates.Add(attribute);
                                    body.AppendValue(attribute, (int)value);
                                }
                            }
                            break;
                        case DataManagementType.TypeInt64:
                            {
                                if (!touch && record.TryGetValue(attribute, out var existing))
                                {
                                    if (existing == null || (long)existing != (long)value)
                                    {
                                        Console.WriteLine(" - Update ...");
                                        updates.Add(attribute);
                                        body.AppendValue(attribute, (long)value);
                                    }
                                }
                                else
                                {
                                    Console.WriteLine(" - Touch ...");
                                    updates.Add(attribute);
                                    body.AppendValue(attribute, (long)value);
                                }
                            }
                            break;
                        case DataManagementType.TypeCollection:
                            //TODO, get delta
                            Console.WriteLine(" - Update (forced) ...");
                            updates.Add(attribute);
                            body.AppendValue(attribute, (List<object>)value);
                            break;
                        default:
                            Console.WriteLine($"WARNING: treating '{type}' for attribute '{attribute}' as value.ToString() --> final implementation missing so far. Therefore it's experimental and may not work ...");
                            body.AppendObject(attribute, value.ToString());
                            break;
                    }
                }
            }
            body.CloseJson();
            if (updates.Count > 0)
            {
                Console.WriteLine($"Update record '{entity}({id:D})' using attributes '{string.Join(",", updates)}'!");
                return Client.Patch(url, body.ToString());
            }
            Console.WriteLine($"Skip update record '{entity}({id:D})', no deltas found!");
            return true;
        }

        private static bool DeleteRecord(string entities, Guid id)
        {
            var url = $"{PsAutomation.ApiUrl}/{entities}({id:D})";
            return Client.Delete(url);
        }

        private static bool GetRecord(string entities, List<KeyValue> attributes, Search search, out Record record)
        {
            var id = search.Id;
            var filter = search.Filter;
            record = null;
            if (((id == null || id == Guid.Empty) && string.IsNullOrWhiteSpace(filter)) ||
                ((id != null && id != Guid.Empty) && !string.IsNullOrWhiteSpace(filter)))
            {
                Console.Error.WriteLine($"ERROR: search invalid, see {search}!");
                return false;
            }

            if (id != null)
            {
                var request = $"{PsAutomation.ApiUrl}/{entities}({id:D})?$select={string.Join(",", attributes.Select(e => e.Attribute))}";
                var result = Client.Get<Record>(request, out var response);
                if (response.StatusCode == 200)
                {
                    record = (Record)response.Content;
                }
                else if (response.StatusCode == 404)
                {
                    return true;
                }
                return result;
            }
            else
            {
                var request = $"{PsAutomation.ApiUrl}/{entities}?$select={string.Join(",", attributes.Select(e => e.Attribute))}&$filter={Uri.EscapeDataString(filter)}&$top=2";
                var result = Client.Fetch<ODataContents<List<Record>>>(request, out var response);
                if (response.StatusCode == 200)
                {
                    var content = (ODataContents<List<Record>>)response.Content;
                    if (content.Value == null)
                    {
                        return true;
                    }
                    if (content.Value.Count != 1)
                    {
                        Console.Error.WriteLine($"ERROR: search invalid, see {search}; result count '{content.Value?.Count}'!");
                        return false;
                    }
                    record = content.Value?.First();
                }
                return result;
            }
        }
    }
}
