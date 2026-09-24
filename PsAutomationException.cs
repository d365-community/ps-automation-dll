using System;

namespace D365.Community.Ps.Automation
{
    internal class PsAutomationException : SystemException
    {
        /// <inheritdoc />
        internal PsAutomationException(string message) : base(message) { }
        /// <inheritdoc />
        internal PsAutomationException(string message, Exception innerException) : base(message, innerException) { }
    }
}
