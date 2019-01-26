using Crawler.AppCore;

namespace Crawler.Infrastructure
{
    public class WebCrawlerFactory
    {
        public static WebCrawler Create(WebCrawlConfiguration configuration)
        {
            return new WebCrawler(configuration, defaultHttpClientFactory, new LinkValidator(configuration.Uri));
        }

        private static IHttpClient defaultHttpClientFactory()
        {
            return new HttpClientAdapter();
        }
    }
}
