using System.Collections.Generic;
using System.Net;

namespace Crawler.AppCore
{
    public class LinkCrawlResult
    {
        public string Url { get; set; }
        public HttpStatusCode StatusCode { get; set; }
        public List<string> Links { get; set; }
    }
}