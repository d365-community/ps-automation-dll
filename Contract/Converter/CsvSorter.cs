using System;
using System.Linq;

namespace D365.Community.Ps.Automation.Contract.Converter
{
    internal static class CsvSorter
    {
        internal static string Transform(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? value : string.Join(",", value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).OrderBy(a => a));
        }
    }
}
