namespace BrowserAutomationMaster.Managers
{
    public class RequestManager(Uri uri, int timeout = 10)
    {
        public HttpClient Client { get; } = NetworkClient.Instance;
        public Uri Uri { get; private set; } = uri;
        public TimeSpan Timeout { get; private set; } = TimeSpan.FromSeconds(timeout);

        public void UpdateUri(Uri uri) { Uri = uri; }
        public void UpdateTimeout(int timeoutSeconds) { Timeout = TimeSpan.FromSeconds(timeoutSeconds); }

        public async Task<HttpResponseMessage> GetAsync()
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, Uri);
            using var cts = new CancellationTokenSource(Timeout);
            return await Client.SendAsync(request, cts.Token);
        }

        public async Task<HttpResponseMessage> GetAsync(bool followRedirects)
        {
            using var specificClient = NetworkClient.GetClientWithRedirectsAllowed(followRedirects);
            using var request = new HttpRequestMessage(HttpMethod.Get, Uri);
            using var cts = new CancellationTokenSource(Timeout);
            return await specificClient.SendAsync(request, cts.Token);
        }


        public async Task<string> GetStringAsync()
        {
            using var response = await GetAsync();
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> GetStringAsync(bool disableRedirectsForThisRequest)
        {
            using var response = await GetAsync(disableRedirectsForThisRequest);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
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

            public static HttpClient GetClientWithRedirectsAllowed(bool allowRedirects)
            {
                var handler = new HttpClientHandler { AllowAutoRedirect = allowRedirects };
                var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };
                client.DefaultRequestHeaders.Add(
                    "User-Agent", "Mozilla/5.5 (Windows NT 10.0; Win64; x64; rv:136.0) Gecko/20100101 Firefox/136.0"
                );
                return client;
            }
        }
    }
}