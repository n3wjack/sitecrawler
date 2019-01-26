using Crawler.AppCore;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Crawler.Infrastructure
{
    public class HttpClientAdapter : IHttpClient, IDisposable
    {
        private HttpClient _client;

        public HttpClientAdapter()
        {
            var clientHandler = new HttpClientHandler
            {
                AllowAutoRedirect = false
            };
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
    }
}
