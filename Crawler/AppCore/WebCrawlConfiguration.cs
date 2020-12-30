using System;

namespace Crawler.AppCore
{
    public class WebCrawlConfiguration
    {
        public Uri Uri { get; set; }
        public int RetryDelay { get; set; } = 1000;
    }
}