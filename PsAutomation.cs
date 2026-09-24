using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Authentication;
using System.Text;
using D365.Community.Ps.Automation.Job;
using D365.Community.Ps.Automation.Security;
using D365.Community.Ps.Automation.Service;
using D365.Community.Ps.Automation.Contract.Config;
using D365.Community.Ps.Automation.Contract.OAuth;
using D365.Community.Ps.Automation.Export;
using D365.Community.Ps.Automation.Net;

namespace D365.Community.Ps.Automation
{
    /// <summary>
    /// PowerShell adapter. This class provides features which are currently missing in pac!
    /// </summary>
    public class PsAutomation
    {
        internal static string DynamicsUrl;
        internal static string ApiUrl;
        internal static OAuthRecord OAuthRecord;
        internal static string CallerObjectId;
        internal static string MsCrmCallerId;

        /// <summary>
        /// Authenticate against login.microsoftonline.com with username and password
        /// </summary>
        /// <param name="pacDir"></param>
        /// <param name="authName"></param>
        /// <param name="url"></param>
        /// <param name="username"></param>
        /// <param name="password"></param>
        public static void AuthCreate(string pacDir, string authName, string url, string username, SecureString password)
        {
            url = url.TrimEnd('/') + "/";
            PersistOAuth(pacDir, authName, url, Client.Auth("https://login.microsoftonline.com/common/oauth2/token", $"grant_type=password&resource={url}&client_id=9cee029c-6210-4654-90bb-17e6e9d36617&username={username}&password={Marshal.PtrToStringAuto(Marshal.SecureStringToBSTR(password))}", OAuthTokenType.UserPwd));
        }

        /// <summary>
        /// Authenticate against login.microsoftonline.com with client id and client secret
        /// </summary>
        /// <param name="pacDir"></param>
        /// <param name="authName"></param>
        /// <param name="url"></param>
        /// <param name="applicationId"></param>
        /// <param name="clientSecret"></param>
        /// <param name="tenant"></param>
        public static void AuthCreate(string pacDir, string authName, string url, string applicationId, SecureString clientSecret, string tenant)
        {
            url = url.TrimEnd('/') + "/";
            PersistOAuth(pacDir, authName, url, Client.Auth($"https://login.microsoftonline.com/{tenant}/oauth2/v2.0/token", $"grant_type=client_credentials&scope={url}.default&client_id={applicationId}&client_secret={Marshal.PtrToStringAuto(Marshal.SecureStringToBSTR(clientSecret))}", OAuthTokenType.AppSecret));
        }

        /// <summary>
        /// Authenticate against login.microsoftonline.com with client id and client certificate
        /// </summary>
        /// <param name="pacDir"></param>
        /// <param name="authName"></param>
        /// <param name="url"></param>
        /// <param name="applicationId"></param>
        /// <param name="certificateDiskPath"></param>
        /// <param name="certificatePassword"></param>
        /// <param name="tenant"></param>
        public static void AuthCreate(string pacDir, string authName, string url, string applicationId, string certificateDiskPath, SecureString certificatePassword, string tenant)
        {
            var psAuth = bool.Parse(Environment.GetEnvironmentVariable("D365_PS_AUTH") ?? "false");
            var loginUrl = $"https://login.microsoftonline.com/{tenant}/oauth2/v2.0/token";

            //client_assertion: An assertion (a JSON web token) that you need to create and sign with the certificate you registered as credentials for your application. Read about certificate credentials to learn how to register your certificate and the format of the assertion.
            //see https://learn.microsoft.com/en-us/entra/identity-platform/certificate-credentials
            var header = new StringBuilder()
                .Append("{ ")
                .Append("\"alg\": \"RS256\", ")
                .Append("\"typ\": \"JWT\", ")
                .Append("\"x5t\": \"")
                .Append(Base64Url.Encode(Cryptography.X509CertificateDER(certificateDiskPath, certificatePassword)))
                .Append("\" }")
                .ToString();
            if (psAuth) Console.WriteLine($"header: '{header}'");
            var headerBase64Url = Base64Url.Encode(header);

            var payload = new StringBuilder()
                .Append("{ ")
                .Append("\"aud\": \"").Append(loginUrl).Append("\", ")
                .Append("\"exp\": \"").Append(((DateTimeOffset)Cryptography.X509CertificateDate(certificateDiskPath, certificatePassword)).ToUnixTimeSeconds()).Append("\", ")
                .Append("\"iss\": \"").Append(applicationId).Append("\", ")
                .Append("\"jti\": \"").Append(Guid.NewGuid().ToString("D")).Append("\", ")
                .Append("\"nbf\": \"").Append(DateTimeOffset.UtcNow.ToUnixTimeSeconds()).Append("\", ")
                .Append("\"sub\": \"").Append(applicationId).Append("\", ")
                .Append("\"iat\": \"").Append(DateTimeOffset.UtcNow.ToUnixTimeSeconds()).Append("\"")
                .Append(" }")
                .ToString();
            if (psAuth) Console.WriteLine($"payload: '{payload}'");
            var payloadBase64Url = Base64Url.Encode(payload);

            var assertion = $"{headerBase64Url}.{payloadBase64Url}.{Cryptography.JWTSignature(headerBase64Url, payloadBase64Url, certificateDiskPath, certificatePassword)}";

            if (psAuth) Console.WriteLine($"assertion: '{assertion}'");

            url = url.TrimEnd('/') + "/";
            PersistOAuth(pacDir, authName, url, Client.Auth(loginUrl, $"grant_type=client_credentials&scope={url}.default&client_id={applicationId}&client_assertion_type=urn:ietf:params:oauth:client-assertion-type:jwt-bearer&client_assertion={assertion}", OAuthTokenType.AppCert));
        }

        public static void AuthDelete(string pacDir, string authName)
        {
            var psDebug = bool.Parse(Environment.GetEnvironmentVariable("D365_PS_DEBUG") ?? "false");

            var store = ReadOAuthStore(pacDir);
            if (store.Data.ContainsKey(authName))
            {
                if (psDebug) Console.WriteLine($"remove {authName}");
                store.Data.Remove(authName);
            }
            if (OAuthRecord != null && OAuthRecord.AuthName == authName) OAuthRecord = null;

            WriteOAuthStore(pacDir, store);
        }

        public static void AuthSelect(string pacDir, string authName)
        {
            var psDebug = bool.Parse(Environment.GetEnvironmentVariable("D365_PS_DEBUG") ?? "false");

            var store = ReadOAuthStore(pacDir);
            foreach (var entry in store.Data.Values.Where(entry => entry.Active))
            {
                if (string.Equals(entry.AuthName, authName)) continue;
                if (psDebug) Console.WriteLine($"inactivate {entry.AuthName}");
                entry.Active = false;
            }

            if (store.Data.TryGetValue(authName, out var record))
            {
                if (psDebug) Console.WriteLine($"activate {authName}");
                record.Active = true;
            }
            else
            {
                throw new Exception($"ps automation failed! {authName} not found!");
            }
            if (OAuthRecord == null || OAuthRecord.AuthName != authName) OAuthRecord = record;

            WriteOAuthStore(pacDir, store);
        }

        public static void AuthDeselect(string pacDir)
        {
            var psDebug = bool.Parse(Environment.GetEnvironmentVariable("D365_PS_DEBUG") ?? "false");

            var store = ReadOAuthStore(pacDir);
            foreach (var entry in store.Data.Values.Where(entry => entry.Active))
            {
                if (psDebug) Console.WriteLine($"inactivate {entry.AuthName}");
                entry.Active = false;
            }

            OAuthRecord = null;

            WriteOAuthStore(pacDir, store);
        }

        /// <summary>
        /// ps automation execution invocation
        /// </summary>
        /// <param name="pacDir"></param>
        /// <param name="directory"></param>
        /// <param name="prefix">optional environment prefix</param>
        /// <param name="arguments">ps automation args</param>
        /// <exception cref="ArgumentNullException">no job specified</exception>
        /// <exception cref="ArgumentOutOfRangeException">no job specified</exception>
        /// <exception cref="AuthenticationException"></exception>
        /// <exception cref="PsAutomationException"></exception>
        public static void Execute(string pacDir, string directory, string prefix, params string[] arguments)
        {
            if (arguments == null)
            {
                throw new ArgumentNullException(nameof(arguments), "no job specified!");
            }
            if (arguments.Length < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(arguments), "no job specified!");
            }

            var psTrace = bool.Parse(Environment.GetEnvironmentVariable("D365_PS_TRACE") ?? "false");
            var psDebug = bool.Parse(Environment.GetEnvironmentVariable("D365_PS_DEBUG") ?? "false");

            var file = Path.Combine(pacDir.TrimEnd('\\'), "d365.dat");
            if (!File.Exists(file))
            {
                throw new AuthenticationException("not authenticated! file not found ...");
            }
            var store = ReadOAuthStore(pacDir);
            OAuthRecord = store.Data.Values.FirstOrDefault(e => e.Active);
            if (OAuthRecord == default)
            {
                throw new AuthenticationException("not authenticated! no active selection found ...");
            }
            if (psTrace) Console.WriteLine($"OAuthRecord: '{OAuthRecord}'");

            DynamicsUrl = OAuthRecord.DynamicsUrl;
            ApiUrl = $"{OAuthRecord.DynamicsUrl}api/data/v9.2";

            bool success;

            if (psDebug) Console.WriteLine($"pac dir: {pacDir}");
            Console.WriteLine($"directory '{directory}'");
            Console.WriteLine($"prefix '{prefix}'");
            Console.WriteLine($"run '{string.Join(" ", arguments)}'");

            var job = arguments.First();
            try
            {
                //please keep alphabetic order here (except for export, add export BEFORE job)
                switch (job)
                {
                    /*
                     * assembly-registration
                     */
                    case "export-assembly-registration":
                        {
                            var config = GetJobData<Caller>(directory, prefix, "assembly-registration", true);
                            CallerObjectId = config?.CallerObjectId;
                            MsCrmCallerId = config?.MsCrmCallerId;
                            var filter = string.Empty;
                            if (arguments.Length > 1)
                            {
                                filter = string.Join(" and ", arguments.Skip(1));
                            }
                            success = AssemblyRegistrationExport.Execute(directory, prefix, filter);
                        }
                        break;
                    case "assembly-registration":
                        {
                            var config = GetJobData<Caller>(directory, prefix, job, true);
                            CallerObjectId = config?.CallerObjectId;
                            MsCrmCallerId = config?.MsCrmCallerId;
                            success = arguments.Length >= 2 && AssemblyRegistrationJob.Execute(directory, prefix, arguments.Skip(1).ToArray());
                        }
                        break;
                    /*
                     * auto-number-format
                     */
                    case "auto-number-format":
                        {
                            var formats = GetJobData<AutoNumberFormats>(directory, prefix, job);
                            CallerObjectId = formats.CallerObjectId;
                            MsCrmCallerId = formats.MsCrmCallerId;
                            success = AutoNumberFormatJob.Execute(formats);
                        }
                        break;
                    /*
                     * bulk-delete
                     */
                    case "export-bulk-delete":
                        {
                            var deletes = GetJobData<BulkDeletes>(directory, prefix, "bulk-delete", true);
                            CallerObjectId = deletes?.CallerObjectId;
                            MsCrmCallerId = deletes?.MsCrmCallerId;
                            var filter = deletes?.Filter;
                            if (string.IsNullOrWhiteSpace(filter) && arguments.Length > 1)
                            {
                                filter = string.Join(" and ", arguments.Skip(1));
                            }
                            success = BulkDeleteExport.Execute(directory, prefix, filter);
                        }
                        break;
                    case "bulk-delete":
                        {
                            var deletes = GetJobData<BulkDeletes>(directory, prefix, job);
                            CallerObjectId = deletes.CallerObjectId;
                            MsCrmCallerId = deletes.MsCrmCallerId;
                            success = BulkDeleteJob.Execute(deletes);
                        }
                        break;
                    /*
                     * component-layer-review
                     */
                    case "component-layer-review":
                        {
                            var componentLayers = GetJobData<ComponentLayers>(directory, prefix, "component-layer");
                            CallerObjectId = componentLayers.CallerObjectId;
                            MsCrmCallerId = componentLayers.MsCrmCallerId;
                            success = ComponentLayersReview.Execute(directory, prefix, componentLayers);
                        }
                        break;
                    /*
                     * connection-reference
                     */
                    case "export-connection-reference":
                        {
                            var config = GetJobData<ConnectionReferencesConfig>(directory, prefix, "connection-reference", true);
                            CallerObjectId = config?.CallerObjectId;
                            MsCrmCallerId = config?.MsCrmCallerId;
                            var filter = config?.Filter;
                            if (string.IsNullOrWhiteSpace(filter) && arguments.Length > 1)
                            {
                                filter = string.Join(" and ", arguments.Skip(1));
                            }
                            success = ConnectionReferenceExport.Execute(directory, prefix, filter);
                        }
                        break;
                    /*
                     * data-management
                     */
                    case "data-management":
                        {
                            var data = GetJobData<DataManagementSet>(directory, prefix, job);
                            CallerObjectId = data?.CallerObjectId;
                            MsCrmCallerId = data?.MsCrmCallerId;
                            success = DataManagementJob.Execute(data);
                        }
                        break;
                    /*
                     * document-template
                     */
                    case "export-document-template":
                        {
                            var templates = GetJobData<DocumentTemplates>(directory, prefix, "document-template", true);
                            CallerObjectId = templates?.CallerObjectId;
                            MsCrmCallerId = templates?.MsCrmCallerId;
                            var filter = templates?.Filter;
                            if (string.IsNullOrWhiteSpace(filter) && arguments.Length > 1)
                            {
                                filter = string.Join(" and ", arguments.Skip(1));
                            }
                            success = DocumentTemplateExport.Execute(directory, prefix, filter);
                        }
                        break;
                    case "document-template":
                        {
                            var templates = GetJobData<DocumentTemplates>(directory, prefix, job);
                            CallerObjectId = templates.CallerObjectId;
                            MsCrmCallerId = templates.MsCrmCallerId;
                            success = DocumentTemplateJob.Execute(directory, templates);
                        }
                        break;
                    /*
                     * duplicate-rule
                     */
                    case "duplicate-rule":
                        {
                            var rules = GetJobData<DuplicateRules>(directory, prefix, job);
                            CallerObjectId = rules.CallerObjectId;
                            MsCrmCallerId = rules.MsCrmCallerId;
                            success = DuplicateRuleJob.Execute(rules);
                        }
                        break;
                    /*
                     * entity-all-assets
                     */
                    case "entity-all-assets":
                        {
                            var config = GetJobData<EntitiesAllAssets>(directory, prefix, job);
                            CallerObjectId = config?.CallerObjectId;
                            MsCrmCallerId = config?.MsCrmCallerId;
                            success = EntityAllAssetsCheck.Execute(config);
                        }
                        break;
                    /*
                     * environment-variable
                     */
                    case "export-environment-variable":
                        {
                            var config = GetJobData<EnvironmentVariableConfig>(directory, prefix, "environment-variable", true);
                            CallerObjectId = config?.CallerObjectId;
                            MsCrmCallerId = config?.MsCrmCallerId;
                            var filter = config?.Filter;
                            if (string.IsNullOrWhiteSpace(filter) && arguments.Length > 1)
                            {
                                filter = string.Join(" and ", arguments.Skip(1));
                            }
                            success = EnvironmentVariableExport.Execute(directory, prefix, filter);
                        }
                        break;
                    case "environment-variable":
                        {
                            var config = GetJobData<Caller>(directory, prefix, job);
                            CallerObjectId = config.CallerObjectId;
                            MsCrmCallerId = config.MsCrmCallerId;
                            var pacSettings = "pac-settings";
                            if (arguments.Length > 1)
                            {
                                pacSettings = string.Join("\\", arguments.Skip(1));
                            }
                            success = EnvironmentVariableJob.Execute(directory, prefix, pacSettings);
                        }
                        break;
                    /*
                     * index-creation
                     */
                    case "index-creation":
                        {
                            var config = GetJobData<Caller>(directory, prefix, job, true);
                            CallerObjectId = config?.CallerObjectId;
                            MsCrmCallerId = config?.MsCrmCallerId;
                            success = EntityKeyIndexCreationVerification.Execute();
                        }
                        break;
                    /*
                     * index-status
                     */
                    case "index-status":
                        {
                            var config = GetJobData<Caller>(directory, prefix, job, true);
                            CallerObjectId = config?.CallerObjectId;
                            MsCrmCallerId = config?.MsCrmCallerId;
                            success = EntityKeyIndexStatusVerification.Execute();
                        }
                        break;
                    /*
                     * process-activation
                     */
                    case "process-activation":
                        {
                            var config = GetJobData<ProcessActivation>(directory, prefix, job);
                            CallerObjectId = config.CallerObjectId;
                            MsCrmCallerId = config.MsCrmCallerId;
                            success = ProcessActivationJob.Execute(config);
                        }
                        break;
                    /*
                     * process-owner
                     */
                    case "process-owner":
                        {
                            var config = GetJobData<ProcessOwner>(directory, prefix, job);
                            CallerObjectId = config.CallerObjectId;
                            MsCrmCallerId = config.MsCrmCallerId;
                            var filter = "";//config?.Filter;
                            if (string.IsNullOrWhiteSpace(filter) && arguments.Length > 1)
                            {
                                filter = string.Join(" and ", arguments.Skip(1));
                            }
                            success = ProcessOwnerJob.Execute(config, filter);
                        }
                        break;
                    /*
                     * queue
                     */
                    case "export-queue":
                        {
                            var queries = GetJobData<Queues>(directory, prefix, "queue", true);
                            CallerObjectId = queries?.CallerObjectId;
                            MsCrmCallerId = queries?.MsCrmCallerId;
                            var filter = queries?.Filter;
                            if (string.IsNullOrWhiteSpace(filter) && arguments.Length > 1)
                            {
                                filter = string.Join(" and ", arguments.Skip(1));
                            }
                            success = QueueExport.Execute(directory, prefix, filter);
                        }
                        break;
                    /*
                     * saved-query
                     */
                    case "export-saved-query":
                        {
                            var queries = GetJobData<SavedQueries>(directory, prefix, "saved-query", true);
                            CallerObjectId = queries?.CallerObjectId;
                            MsCrmCallerId = queries?.MsCrmCallerId;
                            var filter = queries?.Filter;
                            if (string.IsNullOrWhiteSpace(filter) && arguments.Length > 1)
                            {
                                filter = string.Join(" and ", arguments.Skip(1));
                            }
                            success = SavedQueryExport.Execute(directory, prefix, filter);
                        }
                        break;
                    case "saved-query":
                        {
                            var queries = GetJobData<SavedQueries>(directory, prefix, job);
                            CallerObjectId = queries?.CallerObjectId;
                            MsCrmCallerId = queries?.MsCrmCallerId;
                            success = SavedQueryJob.Execute(queries);
                        }
                        break;
                    /*
                     * secure-config
                     */
                    case "secure-config":
                        {
                            var config = GetJobData<Caller>(directory, prefix, job, true);
                            CallerObjectId = config?.CallerObjectId;
                            MsCrmCallerId = config?.MsCrmCallerId;
                            success = arguments.Length == 5 && SecureConfigJob.Execute(arguments[1], arguments[2], arguments[3], arguments[4]);
                        }
                        break;
                    /*
                     * solution-component
                     */
                    case "solution-component":
                        {
                            var config = GetJobData<SolutionsComponents>(directory, prefix, "solution-component");
                            CallerObjectId = config?.CallerObjectId;
                            MsCrmCallerId = config?.MsCrmCallerId;
                            success = SolutionComponentCheck.Execute(config);
                        }
                        break;
                    /*
                     * team-template
                     */
                    case "export-team-template":
                        {
                            var templates = GetJobData<TeamTemplates>(directory, prefix, "team-template", true);
                            CallerObjectId = templates?.CallerObjectId;
                            MsCrmCallerId = templates?.MsCrmCallerId;
                            var filter = templates?.Filter;
                            if (string.IsNullOrWhiteSpace(filter) && arguments.Length > 1)
                            {
                                filter = string.Join(" and ", arguments.Skip(1));
                            }
                            success = TeamTemplateExport.Execute(directory, prefix, filter);
                        }
                        break;
                    case "team-template":
                        {
                            var templates = GetJobData<TeamTemplates>(directory, prefix, job);
                            CallerObjectId = templates?.CallerObjectId;
                            MsCrmCallerId = templates?.MsCrmCallerId;
                            success = TeamTemplateJob.Execute(templates);
                        }
                        break;
                    /*
                     * user-setup
                     */
                    case "user-setup":
                        {
                            var users = GetJobData<UserSet>(directory, prefix, job);
                            CallerObjectId = users.CallerObjectId;
                            MsCrmCallerId = users.MsCrmCallerId;
                            success = UserSetupJob.Execute(users);
                        }
                        break;
                    /*
                     * webresource-registration
                     */
                    case "webresource-registration":
                        {
                            var registrations = GetJobData<WebResourceRegistration>(directory, prefix, job);
                            CallerObjectId = registrations?.CallerObjectId;
                            MsCrmCallerId = registrations?.MsCrmCallerId;
                            success = WebResourceRegistrationJob.Execute(directory, registrations);
                        }
                        break;
                    /*
                     * export-file-attachment
                     */
                    case "export-file-attachment":
                        {
                            var config = GetJobData<Caller>(directory, prefix, job, true);
                            CallerObjectId = config?.CallerObjectId;
                            MsCrmCallerId = config?.MsCrmCallerId;
                            success = arguments.Length == 4 && FileAttachmentExport.Execute(directory, prefix, arguments[1], arguments[2], arguments[3]);
                        }
                        break;
                    default:
                        throw new NotSupportedException($"ps automation {job} unknown!");
                }
            }
            catch (Exception e)
            {
                if (psDebug)
                {
                    throw new PsAutomationException($"ps automation {job} failed! Args: '{string.Join("|", arguments.Skip(1))}'; {e.GetBaseException().Message}; {e.GetBaseException().StackTrace}", e);
                }
                else
                {
                    throw new PsAutomationException($"ps automation {job} failed! Args: '{string.Join("|", arguments.Skip(1))}'; {e.GetBaseException().Message}", e);
                }
            }
            finally
            {
                CallerObjectId = null;
                MsCrmCallerId = null;
            }
            if (!success) throw new PsAutomationException($"ps automation {job} not successful! Args: '{string.Join("|", arguments.Skip(1))}'");
        }

        /// <summary>
        /// read config
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="directory"></param>
        /// <param name="prefix"></param>
        /// <param name="file"></param>
        /// <param name="optional"></param>
        /// <returns></returns>
        /// <exception cref="FileNotFoundException"></exception>
        private static T GetJobData<T>(string directory, string prefix, string file, bool optional = false) where T : Caller
        {
            var path = Path.Combine(directory.TrimEnd('\\'), $"{prefix}{file}.json");
            if (!File.Exists(path))
            {
                path = Path.Combine(directory.TrimEnd('\\'), $"{file}.json");
                if (!File.Exists(path))
                {
                    if (!optional) throw new FileNotFoundException($"Can't find file {path}...", path);
                    path = Path.Combine(directory.TrimEnd('\\'), $"{prefix}caller-id.json");
                    if (!File.Exists(path))
                    {
                        path = Path.Combine(directory.TrimEnd('\\'), "caller-id.json");
                        if (!File.Exists(path))
                        {
                            return default;
                        }
                    }
                }
            }
            return Serializer.JsonDeserialize<T>(File.ReadAllText(path));
        }

        internal static void PersistOAuth(string pacDir, string authName, string url, OAuthRecord record)
        {
            var psDebug = bool.Parse(Environment.GetEnvironmentVariable("D365_PS_DEBUG") ?? "false");

            var store = ReadOAuthStore(pacDir);
            if (store.Data.ContainsKey(authName))
            {
                if (psDebug) Console.WriteLine($"remove {authName}");
                store.Data.Remove(authName);
            }

            foreach (var entry in store.Data.Values.Where(entry => entry.Active))
            {
                if (psDebug) Console.WriteLine($"inactivate {entry.AuthName}");
                entry.Active = false;
            }

            record.Active = true;
            record.Directory = pacDir;
            record.AuthName = authName;
            record.DynamicsUrl = url.TrimEnd('/') + "/";

            if (psDebug) Console.WriteLine($"add {authName}");
            store.Data.Add(authName, record);

            OAuthRecord = record;
            WriteOAuthStore(pacDir, store);
        }

        private static OAuthStore ReadOAuthStore(string pacDir)
        {
            var file = Path.Combine(pacDir.TrimEnd('\\'), "d365.dat");
            //Console.WriteLine($"file {file}");
            return File.Exists(file) ? Serializer.JsonDeserialize<OAuthStore>(Cryptography.Decrypt(File.ReadAllText(file, Encoding.UTF8))) : new OAuthStore();
        }

        private static void WriteOAuthStore(string pacDir, OAuthStore store)
        {
            var file = Path.Combine(pacDir.TrimEnd('\\'), "d365.dat");
            //Console.WriteLine($"file {file}");
            File.WriteAllText(file, Cryptography.Encrypt(Serializer.JsonSerialize(store)), Encoding.UTF8);
        }
    }
}
