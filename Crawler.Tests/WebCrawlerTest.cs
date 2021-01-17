using Crawler.AppCore;
using Moq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Crawler.Tests
{
    /// <summary>
    /// Represents the WebCrawler test base class.
    /// </summary>
    public abstract class WebCrawlerTest
    {
        protected Mock<IHttpClient> HttpClientMock { get; set; } = new Mock<IHttpClient>();

        /// <summary>
        /// Sets up the HTTP requests for a given URL for the WebCrawler.
        /// </summary>
        /// <param name="url">The URL to visit.</param>
        /// <param name="result">The result that should be returned from the requests.</param>
        protected void SetupRequest(string url, HttpResponseMessage result)
        {
            HttpClientMock.Setup(c => c.GetHeadersAsync(url, It.IsAny<CancellationToken>())).Returns(Task.FromResult(result));
            HttpClientMock.Setup(c => c.GetAsync(url, It.IsAny<CancellationToken>())).Returns(Task.FromResult(result));
        }
    }
}