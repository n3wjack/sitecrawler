using Crawler.AppCore;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Crawler.Infrastructure
{
    public class HttpClientAdapter : IHttpClient, IDisposable
    {
        private HttpClient _client;

        public HttpClientAdapter(string username, string password)
        {
            var clientHandler = new HttpClientHandler
            {
                AllowAutoRedirect = false,
            };

            if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
            {
                clientHandler.PreAuthenticate = true;
                clientHandler.Credentials = new NetworkCredential(username, password);
            }

            _client = new HttpClient(clientHandler);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                _client?.Dispose();
            }
        }

        public Task<HttpResponseMessage> GetAsync(string url, CancellationToken token)
        {
            return _client.GetAsync(url, token);
        }

        public Task<HttpResponseMessage> GetHeadersAsync(string url, CancellationToken token)
        {
            return _client.SendAsync(new HttpRequestMessage(HttpMethod.Head, url), token);
        }
    }
}
