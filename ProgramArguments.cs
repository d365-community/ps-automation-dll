using System.Collections.Generic;
using CommandLine;

namespace D365.Community.Ps.Automation
{
    internal class ProgramArguments
    {
        public enum Actions
        {
            None,
            AuthCreate,
            AuthDelete,
            AuthDeselect,
            AuthSelect,
            Execute
        }

        [Option('a', "action", Required = true, HelpText = "action: [AuthCreate|AuthDelete|AuthDeselect|AuthSelect|Execute]")]
        public Actions Action { get; set; }

        [Option('p', "pac", Required = true, HelpText = "pac, e.g. C:\\bin\\pac")]
        public string PacDir { get; set; }

        internal class AuthCreate : ProgramArguments
        {
            [Option("authname", Required = true, HelpText = "authname, e.g. DEV-001")]
            public string AuthName { get; set; }

            [Option("url", Required = true, HelpText = "url, e.g. https://myorg.crm4.dynamics.com")]
            public string Url { get; set; }

            [Option("username", Required = false, HelpText = "username")]
            public string Username { get; set; }

            [Option("password", Required = false, HelpText = "password")]
            public string Password { get; set; }

            [Option("applicationId", Required = false, HelpText = "applicationId")]
            public string ApplicationId { get; set; }

            [Option("clientSecret", Required = false, HelpText = "clientSecret")]
            public string ClientSecret { get; set; }

            [Option("certificateDiskPath", Required = false, HelpText = "certificateDiskPath")]
            public string CertificateDiskPath { get; set; }

            [Option("certificatePassword", Required = false, HelpText = "certificatePassword")]
            public string CertificatePassword { get; set; }

            [Option("tenant", Required = false, HelpText = "tenant")]
            public string Tenant { get; set; }
        }

        internal class AuthDelete : ProgramArguments
        {
            [Option("authname", Required = true, HelpText = "authname, e.g. DEV-001")]
            public string AuthName { get; set; }
        }

        internal class AuthDeselect : ProgramArguments
        {
        }

        internal class AuthSelect : ProgramArguments
        {
            [Option("authname", Required = true, HelpText = "authname, e.g. DEV-001")]
            public string AuthName { get; set; }
        }

        internal class Execute : ProgramArguments
        {
            [Option("directory", Required = true, HelpText = "directory, e.g. C:\\temp")]
            public string Directory { get; set; }

            [Option("prefix", Required = false, Default = "", HelpText = "prefix, e.g. service-")]
            public string Prefix { get; set; }

            [Option("arguments", Required = false, HelpText = "optional arguments")]
            public IEnumerable<string> Arguments { get; set; }
        }
    }
}