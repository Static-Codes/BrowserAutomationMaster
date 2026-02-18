using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static BrowserAutomationMaster.Managers.Common.Constants;
using static BrowserAutomationMaster.Managers.Messaging.Errors;

namespace BrowserAutomationMaster.Managers.Common
{
    public class RequestManager(Uri uri, int timeout = 10)
    {
        public static readonly string DefaultUserAgent = "Mozilla/5.5 (Windows NT 10.0; Win64; x64; rv:136.0) Gecko/20100101 Firefox/146.0";
        public HttpClient Client { get; } = NetworkClient.Instance;
        public Uri Uri { get; private set; } = uri;
        public TimeSpan Timeout { get; private set; } = TimeSpan.FromSeconds(timeout);

        public void UpdateUri(Uri uri) { Uri = uri; }
        public void UpdateTimeout(int timeoutSeconds) { Timeout = TimeSpan.FromSeconds(timeoutSeconds); }

        

        public async Task<HttpResponseMessage?> GetAsync()
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, Uri);
                using var cts = new CancellationTokenSource(Timeout);
                return await Client.SendAsync(request, cts.Token);
            }
            catch
            {
                return null;
            }
        }
        

        public async Task<HttpResponseMessage?> GetAsync(bool followRedirects)
        {
            try
            {
                using var specificClient = NetworkClient.GetClientWithRedirectsAllowed(followRedirects);
                using var request = new HttpRequestMessage(HttpMethod.Get, Uri);
                using var cts = new CancellationTokenSource(Timeout);
                return await specificClient.SendAsync(request, cts.Token);
            }
            catch
            {
                return null;
            }
        }


        public async Task<string?> GetStringAsync()
        {
            try
            {
                using var response = await GetAsync();

                if (response == null) {
                    return null;
                }

                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
            catch
            {
                return null;
            }
        }
        public async Task<string?> GetStringAsyncWithHeaders(Dictionary<string, string> headers, bool ensureStatus = true)
        {
            try
            {
                foreach (var header in headers)
                {
                    NetworkClient.Instance.DefaultRequestHeaders.Add(header.Key, header.Value);
                }

                using var response = await GetAsync();
                
                if (response == null)
                {
                    return null;
                }

                if (ensureStatus)
                {
                    response.EnsureSuccessStatusCode();
                }

                foreach (var header in headers)
                {
                    NetworkClient.Instance.DefaultRequestHeaders.Remove(header.Key);
                }

                return await response.Content.ReadAsStringAsync();
            }
            catch
            {
                return null;
            }
        }


        public async Task<string?> GetStringAsync(bool disableRedirectsForThisRequest)
        {
            using var response = await GetAsync(disableRedirectsForThisRequest);
            if (response == null)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public static async Task<bool> SiteIsPingable(string url) 
        {
            Uri? uri = null;
            try 
            {
                uri = new Uri(url);
            }
            catch (Exception ex) 
            {
                WriteAndExit(
                    message:
                        string.Join(NLC, [
                            $"Unable to determine the availability of the resource at: {url}",
                            "Error Log:",
                            ex.StackTrace ?? ex.Message
                        ]),
                    status: 1
                );
            }

            var message = new HttpRequestMessage(HttpMethod.Head, uri);
            var response = await NetworkClient.Instance.SendAsync(message);
            return response.StatusCode == HttpStatusCode.OK;
        }

        public static RequestManager Create(Uri uri, int timeoutSeconds = 30)
        {
            ArgumentNullException.ThrowIfNull(uri, nameof(uri));
            return new RequestManager(uri, timeoutSeconds);
        }

        public static class NetworkClient
        {
            private static readonly HttpClient _Instance;

            static NetworkClient()
            {
                var defaultHandler = new HttpClientHandler { AllowAutoRedirect = true };
                _Instance = new HttpClient(defaultHandler) { Timeout = TimeSpan.FromSeconds(30) };
                _Instance.DefaultRequestHeaders.Add(
                    "User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:136.0) Gecko/20100101 Firefox/136.0"
                );
            }

            public static HttpClient Instance => _Instance;

            public static async Task<ReadOnlyMemory<byte>> GetReadOnlyMemoryBytesFromURL(string url, int timeout = 30, bool exitOnFail = false)
            {
                try
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeout));

                    using var response = await _Instance.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cts.Token);
                    response.EnsureSuccessStatusCode();

                    long? contentLength = response.Content.Headers.ContentLength;

                    // Preventing an OverflowException by validating contentLength is < 2GB
                    if (contentLength > int.MaxValue)
                    {
                        if (exitOnFail) 
                        {
                            WriteAndExit
                            (
                                message: string.Join(NLC, [
                                    $"An exception occured while attempting to retrieve the response from: {url}",
                                    "Error Log:",
                                    "The provided url returned a contentLength greater than or equal to 2GB, please try again with a smaller download."

                                ]),
                                status: 1
                            ); 
                        }
                        return default; // returns new ReadOnlyMemory<char>
                    }

                    using var stream = await response.Content.ReadAsStreamAsync(cts.Token);
                    
                    var content = contentLength.HasValue ? (int)contentLength.Value : 0;
                    
                    // Buffering the data more efficiently using a MemoryStream.
                    using var memoryStream = new MemoryStream(content);
                    await stream.CopyToAsync(memoryStream, cts.Token);

                    return new ReadOnlyMemory<byte>(memoryStream.ToArray());
                }
                catch (Exception ex)
                {
                    WriteAndExit
                    (
                        message: string.Join(NLC, [
                            $"An exception occured while attempting to retrieve the response from: {url}",
                            "Error Log:",
                            ex.Message

                        ]),
                        status: 1
                    ); 
                }

                return default; // Will never be returned.
            }

            public static async Task<ReadOnlyMemory<char>> GetReadOnlyMemoryCharsFromURL(string url, int timeout = 30, bool exitOnFail = false)
            {
                try
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeout));

                    using var response = await _Instance.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cts.Token);
                    response.EnsureSuccessStatusCode();

                    long? contentLength = response.Content.Headers.ContentLength;

                    // Preventing an OverflowException by validating contentLength is < 2GB
                    if (contentLength > int.MaxValue)
                    {
                        if (exitOnFail) 
                        {
                            WriteAndExit
                            (
                                message: string.Join(NLC, [
                                    $"An exception occured while attempting to retrieve the response from: {url}",
                                    "Error Log:",
                                    "The provided url returned a contentLength greater than or equal to 2GB, please try again with a smaller download."

                                ]),
                                status: 1
                            ); 
                        }
                        return default; // returns new ReadOnlyMemory<char>
                    }

                    using var stream = await response.Content.ReadAsStreamAsync(cts.Token);
                    
                    var content = contentLength.HasValue ? (int)contentLength.Value : 0;
                    
                    // Buffering the data more efficiently using a MemoryStream.
                    using var memoryStream = new MemoryStream(content);
                    await stream.CopyToAsync(memoryStream, cts.Token);
                    
                    var chars = Encoding.UTF8.GetChars(memoryStream.ToArray());
                    return new ReadOnlyMemory<char>(chars);
                }
                catch (Exception ex)
                {
                    WriteAndExit
                    (
                        message: string.Join(NLC, [
                            $"An exception occured while attempting to retrieve the response from: {url}",
                            "Error Log:",
                            ex.Message

                        ]),
                        status: 1
                    ); 
                }

                return default; // Will never be returned.
            }
            

            public static HttpClient GetClientWithRedirectsAllowed(bool allowRedirects, string? userAgent = null)
            {
                if (string.IsNullOrEmpty(userAgent)) {
                    userAgent = DefaultUserAgent;
                }

                var handler = new HttpClientHandler { AllowAutoRedirect = allowRedirects };
                var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };
                client.DefaultRequestHeaders.Add("User-Agent", userAgent);
                return client;
            }

            
        }
        
    }
}