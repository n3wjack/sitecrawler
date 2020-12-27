using Crawler.AppCore;

namespace Crawler.Infrastructure
{
    public class WebCrawlerFactory
    {
        public static WebCrawler Create(WebCrawlConfiguration configuration)
        {
            return new WebCrawler(configuration, DefaultHttpClientFactory);
        }

        private static IHttpClient DefaultHttpClientFactory()
        {
            return new HttpClientAdapter();
        }
    }
}
