using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Crawler.AppCore
{
    public interface IHttpClient : IDisposable
    {
        Task<HttpResponseMessage> GetAsync(string url, CancellationToken token);
        Task<HttpResponseMessage> GetHeadersAsync(string url, CancellationToken token);
    }
}
