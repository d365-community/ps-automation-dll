using System;
using System.Text;

namespace D365.Community.Ps.Automation.Net
{
    internal static class Base64Url
    {
        internal static string Encode(string txt)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(txt)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        internal static string Encode(byte[] txt)
        {
            return Convert.ToBase64String(txt).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }
    }
}
