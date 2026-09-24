using System;
using System.Collections.Generic;
using System.Text;

namespace D365.Community.Ps.Automation.Service
{
    internal static class StringExtensions
    {
        internal static bool IsChanged(string current, string target)
        {
            return !string.Equals($"{current?.Trim()}", $"{target?.Trim()}", StringComparison.InvariantCultureIgnoreCase);
        }

        internal static StringBuilder OpenJson(this StringBuilder self)
        {
            return self.Append("{ ");
        }

        internal static StringBuilder AppendValue(this StringBuilder self, string attribute, string value)
        {
            return self.Append("'").Append(attribute).Append("': '").Append(value?.Replace("'", "\\'")).Append("', ");
        }

        internal static StringBuilder AppendValue(this StringBuilder self, string attribute, Guid? value)
        {
            return self.Append("'").Append(attribute).Append("': '").Append(value?.ToString("D")).Append("', ");
        }

        internal static StringBuilder AppendValue(this StringBuilder self, string attribute, bool value)
        {
            return self.Append("'").Append(attribute).Append("': ").Append(value ? "true" : "false").Append(", ");
        }

        internal static StringBuilder AppendValue(this StringBuilder self, string attribute, int value)
        {
            return self.Append("'").Append(attribute).Append("': ").Append(value).Append(", ");
        }

        internal static StringBuilder AppendValue(this StringBuilder self, string attribute, long value)
        {
            return self.Append("'").Append(attribute).Append("': ").Append(value).Append(", ");
        }

        internal static StringBuilder AppendValue(this StringBuilder self, string attribute, DateTimeOffset value, string format = "yyyy-MM-ddTHH:mm:ssZ")
        {
            return self.Append("'").Append(attribute).Append("': '").Append(value.ToString(format)).Append("', ");
        }

        internal static StringBuilder AppendValue(this StringBuilder self, string attribute, List<object> values)
        {
            return self.Append("'").Append(attribute).Append("': [").Append(string.Join(",", values)).Append("], ");
        }

        internal static StringBuilder AppendNull(this StringBuilder self, string attribute)
        {
            return self.Append("'").Append(attribute).Append("': ").Append("null").Append(", ");
        }

        internal static StringBuilder AppendObject(this StringBuilder self, string attribute, string value)
        {
            return self.Append("'").Append(attribute).Append("': ").Append(value).Append(", ");
        }

        internal static StringBuilder CloseJson(this StringBuilder self)
        {
            //trim comma & blank
            self.Length--;
            self.Length--;
            return self.Append(" }");
        }
    }
}
