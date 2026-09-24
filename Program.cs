using System;
using System.Linq;
using System.Security;
using CommandLine;
using CommandLine.Text;

namespace D365.Community.Ps.Automation
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var parser = new Parser(with =>
            {
                //ignore case for enum values
                with.CaseInsensitiveEnumValues = true;
                with.CaseSensitive = false;
                with.IgnoreUnknownArguments = true;
            });
            var result = parser.ParseArguments<ProgramArguments>(args);
            var action = ProgramArguments.Actions.None;
            var pacDir = string.Empty;

            result
                .WithParsed(arg =>
                {
                    action = arg.Action;
                    pacDir = arg.PacDir;
                })
                .WithNotParsed(errs =>
                {
                    Console.Error.WriteLine("---------- ERROR ----------");
                    var helpText = HelpText.AutoBuild(result,
                        h => HelpText.DefaultParsingErrorsHandler(result, h),
                        e => e);
                    Console.Error.Write(helpText);
                });

            switch (action)
            {
                case ProgramArguments.Actions.AuthCreate:
                    {
                        parser.ParseArguments<ProgramArguments.AuthCreate>(args).WithParsed(arg =>
                            {
                                if (!string.IsNullOrWhiteSpace(arg.Username))
                                {
                                    PsAutomation.AuthCreate(pacDir, arg.AuthName, arg.Url, arg.Username, Secure(arg.Password));
                                }
                                else if (string.IsNullOrWhiteSpace(arg.CertificateDiskPath))
                                {
                                    PsAutomation.AuthCreate(pacDir, arg.AuthName, arg.Url, arg.ApplicationId, Secure(arg.ClientSecret), arg.Tenant);
                                }
                                else
                                {
                                    PsAutomation.AuthCreate(pacDir, arg.AuthName, arg.Url, arg.ApplicationId, arg.CertificateDiskPath, Secure(arg.CertificatePassword), arg.Tenant);
                                }

                                return;

                                SecureString Secure(string secret)
                                {
                                    var secure = new SecureString();
                                    secret.ToCharArray().ToList().ForEach(secure.AppendChar);
                                    secure.MakeReadOnly();
                                    return secure;
                                }
                            })
                            .WithNotParsed(errs =>
                            {
                                Console.Error.WriteLine("---------- ERROR ----------");
                                var helpText = HelpText.AutoBuild(result,
                                    h => HelpText.DefaultParsingErrorsHandler(result, h),
                                    e => e);
                                Console.Error.Write(helpText);
                            });
                    }
                    return;
                case ProgramArguments.Actions.AuthDelete:
                    {
                        parser.ParseArguments<ProgramArguments.AuthDelete>(args).WithParsed(arg =>
                            {
                                PsAutomation.AuthDelete(pacDir, arg.AuthName);
                            })
                            .WithNotParsed(errs =>
                            {
                                Console.Error.WriteLine("---------- ERROR ----------");
                                var helpText = HelpText.AutoBuild(result,
                                    h => HelpText.DefaultParsingErrorsHandler(result, h),
                                    e => e);
                                Console.Error.Write(helpText);
                            });
                    }
                    return;
                case ProgramArguments.Actions.AuthDeselect:
                    {
                        PsAutomation.AuthDeselect(pacDir);
                    }
                    return;
                case ProgramArguments.Actions.AuthSelect:
                    {
                        parser.ParseArguments<ProgramArguments.AuthSelect>(args).WithParsed(arg =>
                            {
                                PsAutomation.AuthSelect(pacDir, arg.AuthName);
                            })
                            .WithNotParsed(errs =>
                            {
                                Console.Error.WriteLine("---------- ERROR ----------");
                                var helpText = HelpText.AutoBuild(result,
                                    h => HelpText.DefaultParsingErrorsHandler(result, h),
                                    e => e);
                                Console.Error.Write(helpText);
                            });
                    }
                    return;
                case ProgramArguments.Actions.Execute:
                    {
                        parser.ParseArguments<ProgramArguments.Execute>(args).WithParsed(arg =>
                            {
                                PsAutomation.Execute(pacDir, arg.Directory, arg.Prefix, arg.Arguments?.ToArray());
                            })
                            .WithNotParsed(errs =>
                            {
                                Console.Error.WriteLine("---------- ERROR ----------");
                                var helpText = HelpText.AutoBuild(result,
                                    h => HelpText.DefaultParsingErrorsHandler(result, h),
                                    e => e);
                                Console.Error.Write(helpText);
                            });
                    }
                    return;
                case ProgramArguments.Actions.None:
                default:
                    return;
            }
        }
    }
}