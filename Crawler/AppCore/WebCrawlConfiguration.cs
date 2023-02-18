using Microsoft.Extensions.Logging;
using System;

namespace Crawler.AppCore
{
    public class WebCrawlConfiguration
    {
        public Uri Uri { get; set; }
        public int RetryDelay { get; set; } = 1000;
        /// <summary>
        /// Gets or set the time to wait after doing a request, to not overload the site.
        /// </summary>
        public int RequestWaitDelay { get; set; }
        /// <summary>
        /// Gets or sets the number of task to use to crawl simultaneously.
        /// </summary>
        public int ParallelTasks { get; set; } = 10;
        /// <summary>
        /// Gets or sets the username for basic authentication.
        /// </summary>
        public string Username { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the password for basic authentication.
        /// </summary>
        public string Password { get; set; } = string.Empty;
    }
}