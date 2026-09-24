using D365.Community.Ps.Automation.Service;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Authentication;
using System.Text;
using D365.Community.Ps.Automation.Contract.Content;
using D365.Community.Ps.Automation.Contract.OAuth;
using System.Threading;

namespace D365.Community.Ps.Automation
{
    internal class Client
    {
        internal static readonly Random Random = new Random();

        private const int Timeout = 1200000;//20min

        //https://docs.microsoft.com/de-de/powerapps/developer/common-data-service/best-practices/business-logic/set-keepalive-false-interacting-external-hosts-plugin
        //by default, Lazy objects are thread-safe.
        private static readonly Lazy<HttpClient> Lazy = new Lazy<HttpClient>(() =>
        {
            var client = new HttpClient
            {
                Timeout = TimeSpan.FromMilliseconds(Timeout)
            };
            client.DefaultRequestHeaders.ConnectionClose = true;
            return client;
        });

        internal static HttpClient Http => Lazy.Value;

        internal static OAuthRecord Auth(string url, string content, OAuthTokenType type)
        {
            var psAuth = bool.Parse(Environment.GetEnvironmentVariable("D365_PS_AUTH") ?? "false");
            if (psAuth) Console.WriteLine($"content: '{content}'");
            var response = RetryHandler(() => Http.SendAsync(GetAuthRequest()).ConfigureAwait(false).GetAwaiter().GetResult());
            if (psAuth) Console.WriteLine($"response: '{response}'");
            OAuthRecord oauth;
            using (response)
            {
                var json = response.Content.ReadAsStringAsync().Result;
                if (psAuth) Console.WriteLine($"json: '{json}'");
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    oauth = new OAuthRecord
                    {
                        TokenType = type,
                        Content = content,
                        LoginUrl = url
                    };
                    switch (type)
                    {
                        case OAuthTokenType.UserPwd:
                            {
                                var token = Serializer.JsonDeserialize<OAuthUserPwdToken>(json);
                                oauth.AccessToken = token.AccessToken;
                                oauth.ExpiresOn = DateTime.UtcNow.AddSeconds(token.ExpiresIn - 30);
                                break;
                            }

                        case OAuthTokenType.AppSecret:
                            {
                                var token = Serializer.JsonDeserialize<OAuthAppSecretToken>(json);
                                oauth.AccessToken = token.AccessToken;
                                oauth.ExpiresOn = DateTime.UtcNow.AddSeconds(token.ExpiresIn - 30);
                                break;
                            }
                        case OAuthTokenType.AppCert:
                            {
                                var token = Serializer.JsonDeserialize<OAuthAppCertToken>(json);
                                oauth.AccessToken = token.AccessToken;
                                oauth.ExpiresOn = DateTime.UtcNow.AddSeconds(token.ExpiresIn - 30);
                                break;
                            }
                    }
                }
                else
                {
                    throw new AuthenticationException(json);
                }
            }
            return oauth;

            HttpRequestMessage GetAuthRequest()
            {
                var request = new HttpRequestMessage(HttpMethod.Post, new Uri(url));
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                request.Headers.AcceptCharset.Add(new StringWithQualityHeaderValue("utf-8"));
                request.Properties["RequestTimeout"] = TimeSpan.FromMilliseconds(30000);//should not take longer then 30sec
                request.Content = new StringContent(content);
                request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/x-www-form-urlencoded");
                request.Content.Headers.ContentLength = Encoding.UTF8.GetBytes(content).Length;
                return request;
            }
        }

        internal static HttpRequestMessage GetRequest(HttpMethod method, Uri uri, string accept = "application/json")
        {
            if (PsAutomation.OAuthRecord.ExpiresOn < DateTime.UtcNow)
            {
                PsAutomation.PersistOAuth(PsAutomation.OAuthRecord.Directory, PsAutomation.OAuthRecord.AuthName, PsAutomation.OAuthRecord.DynamicsUrl, Auth(PsAutomation.OAuthRecord.LoginUrl, PsAutomation.OAuthRecord.Content, PsAutomation.OAuthRecord.TokenType));
            }
            var accessToken = PsAutomation.OAuthRecord.AccessToken;
            var request = new HttpRequestMessage(method, uri);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(accept));
            request.Headers.AcceptCharset.Add(new StringWithQualityHeaderValue("utf-8"));
            if (!string.IsNullOrWhiteSpace(PsAutomation.CallerObjectId)) request.Headers.Add("CallerObjectId", PsAutomation.CallerObjectId);
            if (!string.IsNullOrWhiteSpace(PsAutomation.MsCrmCallerId)) request.Headers.Add("MSCRMCallerID", PsAutomation.MsCrmCallerId);
            request.Properties["RequestTimeout"] = TimeSpan.FromMilliseconds(Timeout);
            return request;
        }

        internal static ODataResponse GetResponse<T>(HttpResponseMessage response)
        {
            var psTrace = bool.Parse(Environment.GetEnvironmentVariable("D365_PS_TRACE") ?? "false");
            var json = response.Content.ReadAsStringAsync().Result;
            if (psTrace) Console.WriteLine(json);
            ODataResponse result;
            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    result = new ODataResponse
                    {
                        StatusCode = (int)response.StatusCode,
                        Content = Serializer.JsonDeserialize<T>(json),
                        Json = json
                    };
                    break;
                case HttpStatusCode.NoContent:
                    result = new ODataResponse
                    {
                        StatusCode = (int)response.StatusCode,
                        Content = default(NoContent),
                        Json = null
                    };
                    break;
                case HttpStatusCode.BadRequest:
                case HttpStatusCode.Unauthorized:
                case HttpStatusCode.Forbidden:
                case HttpStatusCode.NotFound:
                case HttpStatusCode.InternalServerError:
                    {
                        result = new ODataResponse
                        {
                            StatusCode = (int)response.StatusCode,
                            Content = Serializer.JsonDeserialize<ODataError>(json),
                            Json = json
                        };
                        break;
                    }
                default:
                    {
                        result = new ODataResponse
                        {
                            StatusCode = (int)response.StatusCode,
                            Content = null,
                            Json = json
                        };
                        break;
                    }
            }
            return result;
        }

        /// <summary>
        /// HTTP GET
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="url"></param>
        /// <param name="response"></param>
        /// <returns></returns>
        internal static bool Get<T>(string url, out ODataResponse response) where T : IODataContent
        {
            var psTrace = bool.Parse(Environment.GetEnvironmentVariable("D365_PS_TRACE") ?? "false");
            var psDebug = bool.Parse(Environment.GetEnvironmentVariable("D365_PS_DEBUG") ?? "false");
            if (psDebug) Console.WriteLine(url);
            var result = RetryHandler(() => Http.SendAsync(GetRequest(HttpMethod.Get, new Uri(url))).ConfigureAwait(false).GetAwaiter().GetResult());
            using (result)
            {
                response = GetResponse<T>(result);
            }

            if (response.StatusCode >= 400 & response.StatusCode <= 500)
            {
                if (!psDebug) Console.Error.WriteLine(url);
                Console.Error.WriteLine($"ERROR(Http {response.StatusCode}): {((IODataError)response.Content).GetErrorMessage()}");
                if (!psTrace) Console.Error.WriteLine(response.Json);
                return false;
            }
            if (response.StatusCode != 200)
            {
                if (!psDebug) Console.Error.WriteLine(url);
                Console.Error.WriteLine(!psTrace
                    ? $"ERROR(Http {response.StatusCode})"
                    : $"ERROR(Http {response.StatusCode}): {response.Json}");
                return false;
            }
            return true;
        }

        /// <summary>
        /// HTTP GET
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="url"></param>
        /// <param name="response"></param>
        /// <returns></returns>
        internal static bool Fetch<T>(string url, out ODataResponse response) where T : IODataContents
        {
            var succeeded = true;
            var psTrace = bool.Parse(Environment.GetEnvironmentVariable("D365_PS_TRACE") ?? "false");
            var psDebug = bool.Parse(Environment.GetEnvironmentVariable("D365_PS_DEBUG") ?? "false");
            if (psDebug) Console.WriteLine(url);
            var result = RetryHandler(() => Http.SendAsync(GetRequest(HttpMethod.Get, new Uri(url))).ConfigureAwait(false).GetAwaiter().GetResult());
            using (result)
            {
                response = GetResponse<T>(result);
            }

            if (response.StatusCode >= 400 & response.StatusCode <= 500)
            {
                if (!psDebug) Console.Error.WriteLine(url);
                Console.Error.WriteLine($"ERROR(Http {response.StatusCode}): {((IODataError)response.Content).GetErrorMessage()}");
                if (!psTrace) Console.Error.WriteLine(response.Json);
                succeeded = false;
            }
            else if (response.StatusCode != 200)
            {
                if (!psDebug) Console.Error.WriteLine(url);
                Console.Error.WriteLine(!psTrace
                    ? $"ERROR(Http {response.StatusCode})"
                    : $"ERROR(Http {response.StatusCode}): {response.Json}");
                succeeded = false;
            }
            else
            {
                var nextLink = ((T)response.Content).NextLink();
                while (!string.IsNullOrEmpty(nextLink))
                {
                    if (psDebug) Console.WriteLine(nextLink);
                    var link = nextLink;
                    var nextResult = RetryHandler(() => Http.SendAsync(GetRequest(HttpMethod.Get, new Uri(link))).ConfigureAwait(false).GetAwaiter().GetResult());
                    using (nextResult)
                    {
                        var nextResponse = GetResponse<T>(nextResult);
                        if (nextResponse.StatusCode >= 400 & nextResponse.StatusCode <= 500)
                        {
                            if (!psDebug) Console.Error.WriteLine(link);
                            Console.Error.WriteLine($"ERROR(Http {nextResponse.StatusCode}): {((IODataError)nextResponse.Content).GetErrorMessage()}");
                            if (!psTrace) Console.Error.WriteLine(nextResponse.Json);
                            succeeded = false;
                            break;
                        }
                        if (nextResponse.StatusCode != 200)
                        {
                            if (!psDebug) Console.Error.WriteLine(link);
                            Console.Error.WriteLine(!psTrace
                                ? $"ERROR(Http {nextResponse.StatusCode})"
                                : $"ERROR(Http {nextResponse.StatusCode}): {nextResponse.Json}");
                            succeeded = false;
                            break;
                        }
                        var nextContent = (T)nextResponse.Content;
                        nextLink = nextContent.NextLink();
                        ((T)response.Content).AddRange(nextContent.GetList());
                    }
                }
            }

            if (psDebug) Console.WriteLine($"count: {((T)response.Content).Count()}");

            return succeeded;
        }

        /// <summary>
        /// HTTP PATCH
        /// </summary>
        /// <param name="url"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        internal static bool Patch(string url, string content)
        {
            var psTrace = bool.Parse(Environment.GetEnvironmentVariable("D365_PS_TRACE") ?? "false");
            var psDebug = bool.Parse(Environment.GetEnvironmentVariable("D365_PS_DEBUG") ?? "false");
            if (psDebug)
            {
                Console.WriteLine(url);
                Console.WriteLine(content);
            }
            var result = RetryHandler(() => Http.SendAsync(GetPatchRequest()).ConfigureAwait(false).GetAwaiter().GetResult());
            ODataResponse response;
            using (result)
            {
                response = GetResponse<NoContent>(result);
            }
            if (response.StatusCode >= 400 & response.StatusCode <= 500)
            {
                if (!psDebug)
                {
                    Console.WriteLine(url);
                    Console.WriteLine(content);
                }
                Console.Error.WriteLine($"ERROR(Http {response.StatusCode}): {((IODataError)response.Content).GetErrorMessage()}");
                if (!psTrace) Console.Error.WriteLine(response.Json);
                return false;
            }
            if (response.StatusCode != 204 && response.StatusCode != 200)
            {
                if (!psDebug)
                {
                    Console.WriteLine(url);
                    Console.WriteLine(content);
                }
                Console.Error.WriteLine(!psTrace
                    ? $"ERROR(Http {response.StatusCode})"
                    : $"ERROR(Http {response.StatusCode}): {response.Json}");
                return false;
            }
            return true;

            HttpRequestMessage GetPatchRequest()
            {
                var request = GetRequest(new HttpMethod("PATCH"), new Uri(url));
                request.Content = new StringContent(content);
                request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json; charset=utf-8");
                request.Content.Headers.Add("OData-MaxVersion", "4.0");
                request.Content.Headers.Add("OData-Version", "4.0");
                request.Content.Headers.ContentLength = Encoding.UTF8.GetBytes(content).Length;
                return request;
            }
        }

        /// <summary>
        /// HTTP PATCH XML (deprecated)
        /// </summary>
        /// <param name="url"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        [Obsolete]
        internal static bool PatchXml(string url, string content)
        {
            var psTrace = bool.Parse(Environment.GetEnvironmentVariable("D365_PS_TRACE") ?? "false");
            var psDebug = bool.Parse(Environment.GetEnvironmentVariable("D365_PS_DEBUG") ?? "false");
            if (psDebug)
            {
                Console.WriteLine(url);
                Console.WriteLine(content);
            }
            var result = RetryHandler(() => Http.SendAsync(GetPatchXmlRequest()).ConfigureAwait(false).GetAwaiter().GetResult());
            int statusCode;
            string reason;
            string response;
            using (result)
            {
                statusCode = (int)result.StatusCode;
                reason = result.ReasonPhrase;
                response = result.Content.ReadAsStringAsync().Result;
            }
            if (statusCode >= 400 & statusCode <= 500)
            {
                if (!psDebug)
                {
                    Console.WriteLine(url);
                    Console.WriteLine(content);
                }
                Console.Error.WriteLine($"ERROR(Http {statusCode}): {reason}");
                if (!psTrace) Console.Error.WriteLine(response);
                return false;
            }
            if (statusCode != 204 && statusCode != 200)
            {
                if (!psDebug)
                {
                    Console.WriteLine(url);
                    Console.WriteLine(content);
                }
                Console.Error.WriteLine($"ERROR(Http {statusCode}): {reason}");
                if (!psTrace) Console.Error.WriteLine(response);
                return false;
            }
            return true;

            HttpRequestMessage GetPatchXmlRequest()
            {
                var request = GetRequest(new HttpMethod("PATCH"), new Uri(url), "*/*");
                request.Headers.Add("SOAPAction", "http://schemas.microsoft.com/xrm/2011/Contracts/Services/IOrganizationService/Execute");
                request.Content = new StringContent(content);
                request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("text/xml; charset=utf-8");
                return request;
            }
        }

        /// <summary>
        /// HTTP PUT
        /// </summary>
        /// <param name="url"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        internal static bool Put(string url, string content)
        {
            var psTrace = bool.Parse(Environment.GetEnvironmentVariable("D365_PS_TRACE") ?? "false");
            var psDebug = bool.Parse(Environment.GetEnvironmentVariable("D365_PS_DEBUG") ?? "false");
            if (psDebug)
            {
                Console.WriteLine(url);
                Console.WriteLine(content);
            }
            var result = RetryHandler(() => Http.SendAsync(GetPutRequest()).ConfigureAwait(false).GetAwaiter().GetResult());
            ODataResponse response;
            using (result)
            {
                response = GetResponse<NoContent>(result);
            }
            if (response.StatusCode >= 400 & response.StatusCode <= 500)
            {
                if (!psDebug)
                {
                    Console.WriteLine(url);
                    Console.WriteLine(content);
                }
                Console.Error.WriteLine($"ERROR(Http {response.StatusCode}): {((IODataError)response.Content).GetErrorMessage()}");
                if (!psTrace) Console.Error.WriteLine(response.Json);
                return false;
            }
            if (response.StatusCode != 204 && response.StatusCode != 200)
            {
                if (!psDebug)
                {
                    Console.WriteLine(url);
                    Console.WriteLine(content);
                }
                Console.Error.WriteLine(!psTrace
                    ? $"ERROR(Http {response.StatusCode})"
                    : $"ERROR(Http {response.StatusCode}): {response.Json}");
                return false;
            }
            return true;

            HttpRequestMessage GetPutRequest()
            {
                var request = GetRequest(HttpMethod.Put, new Uri(url));
                request.Content = new StringContent(content);
                request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json; charset=utf-8");
                request.Content.Headers.Add("OData-MaxVersion", "4.0");
                request.Content.Headers.Add("OData-Version", "4.0");
                request.Content.Headers.ContentLength = Encoding.UTF8.GetBytes(content).Length;
                return request;
            }
        }

        /// <summary>
        /// HTTP DELETE
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        internal static bool Delete(string url)
        {
            var psTrace = bool.Parse(Environment.GetEnvironmentVariable("D365_PS_TRACE") ?? "false");
            var psDebug = bool.Parse(Environment.GetEnvironmentVariable("D365_PS_DEBUG") ?? "false");
            if (psDebug) Console.WriteLine(url);
            var result = RetryHandler(() => Http.SendAsync(GetRequest(HttpMethod.Delete, new Uri(url))).ConfigureAwait(false).GetAwaiter().GetResult());
            ODataResponse response;
            using (result)
            {
                response = GetResponse<NoContent>(result);
            }
            if (response.StatusCode >= 400 & response.StatusCode <= 500)
            {
                if (!psDebug) Console.Error.WriteLine(url);
                Console.Error.WriteLine($"ERROR(Http {response.StatusCode}): {((IODataError)response.Content).GetErrorMessage()}");
                if (!psTrace) Console.Error.WriteLine(response.Json);
                return false;
            }
            if (response.StatusCode != 204 && response.StatusCode != 200)
            {
                if (!psDebug) Console.Error.WriteLine(url);
                Console.Error.WriteLine(!psTrace
                    ? $"ERROR(Http {response.StatusCode})"
                    : $"ERROR(Http {response.StatusCode}): {response.Json}");
                return false;
            }
            return true;
        }

        /// <summary>
        /// HTTP POST
        /// </summary>
        /// <param name="url"></param>
        /// <param name="content"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        internal static bool Post(string url, string content, out Guid id)
        {
            id = Guid.Empty;
            var psTrace = bool.Parse(Environment.GetEnvironmentVariable("D365_PS_TRACE") ?? "false");
            var psDebug = bool.Parse(Environment.GetEnvironmentVariable("D365_PS_DEBUG") ?? "false");
            if (psDebug)
            {
                Console.WriteLine(url);
                Console.WriteLine(content);
            }
            var result = RetryHandler(() => Http.SendAsync(GetPostRequest()).ConfigureAwait(false).GetAwaiter().GetResult());
            ODataResponse response;
            using (result)
            {
                response = GetResponse<NoContent>(result);
            }
            if (response != null)
            {
                if (response.StatusCode >= 400 & response.StatusCode <= 500)
                {
                    if (!psDebug)
                    {
                        Console.WriteLine(url);
                        Console.WriteLine(content);
                    }
                    Console.Error.WriteLine($"ERROR(Http {response.StatusCode}): {((IODataError)response.Content).GetErrorMessage()}");
                    if (!psTrace) Console.Error.WriteLine(response.Json);
                    return false;
                }
                if (response.StatusCode != 204 && response.StatusCode != 200)
                {
                    if (!psDebug)
                    {
                        Console.WriteLine(url);
                        Console.WriteLine(content);
                    }
                    Console.Error.WriteLine(!psTrace
                        ? $"ERROR(Http {response.StatusCode})"
                        : $"ERROR(Http {response.StatusCode}): {response.Json}");
                    return false;
                }
            }
            if (result.Headers.TryGetValues("OData-EntityId", out var values))
            {
                id = Guid.Parse(values.First().Split(new[] { '(', ')' }, StringSplitOptions.RemoveEmptyEntries)[1]);
            }
            return true;

            HttpRequestMessage GetPostRequest()
            {
                var request = GetRequest(HttpMethod.Post, new Uri(url));
                request.Content = new StringContent(content);
                request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json; charset=utf-8");
                request.Content.Headers.Add("OData-MaxVersion", "4.0");
                request.Content.Headers.Add("OData-Version", "4.0");
                request.Content.Headers.ContentLength = Encoding.UTF8.GetBytes(content).Length;
                return request;
            }
        }

        /// <summary>
        /// HTTP POST
        /// </summary>
        /// <param name="url"></param>
        /// <param name="content"></param>
        /// <param name="response"></param>
        /// <returns></returns>
        internal static bool Post<T>(string url, string content, out ODataResponse response) where T : IODataContent
        {
            var psTrace = bool.Parse(Environment.GetEnvironmentVariable("D365_PS_TRACE") ?? "false");
            var psDebug = bool.Parse(Environment.GetEnvironmentVariable("D365_PS_DEBUG") ?? "false");
            if (psDebug)
            {
                Console.WriteLine(url);
                Console.WriteLine(content);
            }
            var result = RetryHandler(() => Http.SendAsync(GetPostRequest()).ConfigureAwait(false).GetAwaiter().GetResult());
            using (result)
            {
                response = GetResponse<T>(result);
            }
            if (response != null)
            {
                if (response.StatusCode >= 400 & response.StatusCode <= 500)
                {
                    if (!psDebug)
                    {
                        Console.WriteLine(url);
                        Console.WriteLine(content);
                    }
                    Console.Error.WriteLine($"ERROR(Http {response.StatusCode}): {((IODataError)response.Content).GetErrorMessage()}");
                    if (!psTrace) Console.Error.WriteLine(response.Json);
                    return false;
                }
                if (response.StatusCode != 204 && response.StatusCode != 200)
                {
                    if (!psDebug)
                    {
                        Console.WriteLine(url);
                        Console.WriteLine(content);
                    }
                    Console.Error.WriteLine(!psTrace
                        ? $"ERROR(Http {response.StatusCode})"
                        : $"ERROR(Http {response.StatusCode}): {response.Json}");
                    return false;
                }
            }
            return true;

            HttpRequestMessage GetPostRequest()
            {
                var request = GetRequest(HttpMethod.Post, new Uri(url));
                request.Content = new StringContent(content);
                request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json; charset=utf-8");
                request.Content.Headers.Add("OData-MaxVersion", "4.0");
                request.Content.Headers.Add("OData-Version", "4.0");
                request.Content.Headers.ContentLength = Encoding.UTF8.GetBytes(content).Length;
                return request;
            }
        }

        private static HttpResponseMessage RetryHandler(Func<HttpResponseMessage> func, int maxRetry = 3)
        {
            var attempt = 0;
            do
            {
                attempt++;
                try
                {
                    var response = func.Invoke();
                    if (response != null &&
                        response.StatusCode != HttpStatusCode.GatewayTimeout &&
                        response.StatusCode != HttpStatusCode.InternalServerError &&
                        response.StatusCode != HttpStatusCode.ServiceUnavailable)
                    {
                        return response;
                    }
                    Console.WriteLine($"WARNING(Http {response?.StatusCode}): {response?.ReasonPhrase}");
                    if (attempt >= maxRetry) return response;
                }
                //catch (HttpRequestException hre)
                //catch (TaskCanceledException tce)
                catch (Exception e)
                {
                    Console.Error.WriteLine($"ERROR: {e.GetBaseException().Message}");
                    Thread.Sleep(Random.Next(2873, 7523));
                    if (attempt >= maxRetry) throw;
                }
            } while (true);
        }
    }
}
