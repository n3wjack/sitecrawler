using System;
using System.Collections.Generic;
using System.Net;

namespace Crawler.AppCore
{
    public class LinkCrawlResult
    {
        public bool RequestFailed { get; set; }
        public string Url { get; set; } = string.Empty;
        public HttpStatusCode StatusCode { get; set; }
        public List<string> Links { get; set; } = new List<string>();
        public string ReferrerUrl { get; set; } = string.Empty;
        public string ExceptionMessage { get; set; }
    }
}