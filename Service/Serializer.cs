using System.IO;
using System.Runtime.Serialization.Json;
using System.Runtime.Serialization;
using System.Text;
using System;

namespace D365.Community.Ps.Automation.Service
{
    /// <summary>
    /// default/simple json data serializer<br />
    /// --> keep it simple and stupid
    /// </summary>
    internal static class Serializer
    {
        internal static DataContractJsonSerializerSettings Settings = new DataContractJsonSerializerSettings
        {
            UseSimpleDictionaryFormat = true,
            DateTimeFormat = new DateTimeFormat("yyyy-MM-ddTHH:mm:ssZ")
        };

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <param name="pretty"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        internal static string JsonSerialize<T>(T data, bool pretty = true, DataContractJsonSerializerSettings settings = default)
        {
            if (data == null) return null;
            if (settings == default)
            {
                settings = Settings;
            }
            try
            {
                if (pretty)
                {
                    using (var ms = new MemoryStream())
                    {
                        using (var writer = JsonReaderWriterFactory.CreateJsonWriter(ms, Encoding.UTF8, true, true, "    "))
                        {
                            new DataContractJsonSerializer(typeof(T), settings).WriteObject(writer, data);
                            writer.Flush();
                            var json = ms.ToArray();
                            return Encoding.UTF8.GetString(json, 0, json.Length);
                        }
                    }
                }
                else
                {
                    using (var ms = new MemoryStream())
                    {
                        new DataContractJsonSerializer(typeof(T), settings).WriteObject(ms, data);
                        var json = ms.ToArray();
                        return Encoding.UTF8.GetString(json, 0, json.Length);
                    }
                }
            }
            catch
            {
                Console.Error.WriteLine($"ERROR: {typeof(T).Name} not valid");
                throw;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="json"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        internal static T JsonDeserialize<T>(string json, DataContractJsonSerializerSettings settings = default)
        {
            if (string.IsNullOrWhiteSpace(json)) return default;
            try
            {

                if (settings == default)
                {
                    settings = Settings;
                }
                using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                {
                    return (T)new DataContractJsonSerializer(typeof(T), settings).ReadObject(ms);
                }
            }
            catch
            {
                Console.Error.WriteLine($"ERROR: {typeof(T).Name} not valid for {json}");
                throw;
            }
        }
    }
}