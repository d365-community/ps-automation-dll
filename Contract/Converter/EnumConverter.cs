using System;

namespace D365.Community.Ps.Automation.Contract.Converter
{
    internal static class EnumConverter
    {
        internal static T Convert<T>(string value)
        {
            return (T)Enum.Parse(typeof(T), value, true);
        }

        internal static string Convert<T>(T value)
        {
            return Enum.GetName(typeof(T), value);
        }
    }
}
