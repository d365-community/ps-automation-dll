namespace D365.Community.Ps.Automation.Contract.Content
{
    internal class ODataResponse
    {
        internal int StatusCode { get; set; }

        internal dynamic Content { get; set; }

        internal string Json { get; set; }
    }
}
