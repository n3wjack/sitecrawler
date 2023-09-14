using Crawler.AppCore;
using Microsoft.Extensions.Logging;

namespace Crawler.Infrastructure
{
    public class WebCrawlerFactory
    {
        public static WebCrawler Create(WebCrawlConfiguration configuration, ILogger logger)
        {
            return new WebCrawler(configuration, () => new HttpClientAdapter(configuration.Username, configuration.Password, configuration.CustomUserAgent), logger);
        }
    }
}
