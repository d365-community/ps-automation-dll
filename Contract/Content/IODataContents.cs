using System.Collections;

namespace D365.Community.Ps.Automation.Contract.Content
{
    internal interface IODataContents
    {
        string NextLink();

        void AddRange(IList range);

        IList GetList();

        int Count();
    }
}
