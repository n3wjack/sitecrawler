using HtmlAgilityPack;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Crawler.AppCore
{
    public class WebCrawler
    {
        private ConcurrentDictionary<string, LinkCrawlResult> _linkCrawlResults = new ConcurrentDictionary<string, LinkCrawlResult>();
        private WebCrawlConfiguration _configuration;
        private ILinkValidator _linkValidator;
        private Func<IHttpClient> _httpClientFactory;
        private ParallelOptions _parallelOptions;
        private CancellationTokenSource _cancellationTokenSource;

        public event EventHandler<LinkCrawlResult> LinkCrawled;

        ~WebCrawler()
        {
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Dispose();
            }
        }

        public WebCrawler(WebCrawlConfiguration configuration, Func<IHttpClient> httpClientFactory, ILinkValidator linkValidator)
        {
            _configuration = configuration;
            _linkValidator = linkValidator;
            _httpClientFactory = httpClientFactory;
        }

        public IList<LinkCrawlResult> Start()
        {
            SetOptions();

            CrawlLink(_configuration.Uri.ToString(), _parallelOptions.CancellationToken);

            return _linkCrawlResults.Values.ToList();
        }

        public void Stop()
        {
            _cancellationTokenSource.Cancel();
        }

        private void SetOptions()
        {
            _parallelOptions = new ParallelOptions();
            _cancellationTokenSource = new CancellationTokenSource();
            _parallelOptions.CancellationToken = _cancellationTokenSource.Token;
        }

        private bool LinkNotCrawledYet(string link)
        {
            return !_linkCrawlResults.ContainsKey(link);
        }

        private bool LinkAlreadyCrawled(string link)
        {
            return _linkCrawlResults.ContainsKey(link);
        }

        private void AddCrawlResult(LinkCrawlResult crawlResult)
        {
            LinkCrawled?.Invoke(this, crawlResult);

            // If it already exists, some thread beat us to it.
            _linkCrawlResults.TryAdd(crawlResult.Url, crawlResult);
        }

        private void CrawlLink(string url, CancellationToken token)
        {
            var crawlResult = new LinkCrawlResult { Url = url };

            // Add an empty crawlresult to avoid other threads from also crawling the same url.
            if (!_linkCrawlResults.TryAdd(url, crawlResult))
            {
                Console.WriteLine("=== Already crawled " + url);
                return;
            }

            if (token.IsCancellationRequested)
            {
                Console.WriteLine(" ---- Crawling stopped!! Aborting!");
                return;
            }

            if (url.StartsWith("/"))
            {
                url = $"{_configuration.Uri.Scheme}://{_configuration.Uri.Host}{url}";
            }

            using (var client = _httpClientFactory())
            {
                try
                {
                    var response = client.GetAsync(url, token).Result;
                    var links = ExtractLinks(response).Result.Distinct().ToList();

                    crawlResult.Links = links;
                    crawlResult.StatusCode = response.StatusCode;
                    AddCrawlResult(crawlResult);

                    if (!token.IsCancellationRequested)
                    {
                        Parallel.ForEach(links, _parallelOptions, (link) => CrawlLink(link, token));
                    }
                }
                catch (OperationCanceledException e)
                {
                    Console.WriteLine("Cancelled : " + e.Message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"-- CRAWL ERROR on {url} : {ex.Message}");
                    throw;
                }
            }
        }

        private async Task<List<string>> ExtractLinks(HttpResponseMessage response)
        {
            var resultLinks = new List<string>();

            if (IsRedirect(response.StatusCode))
            {
                var redirectUri = response.Headers.Location?.AbsoluteUri;
                if (redirectUri != null)
                {
                    resultLinks.Add(redirectUri);
                }
            }
            else
            {
                var doc = new HtmlDocument();
                doc.LoadHtml(await response.Content.ReadAsStringAsync());
                var links = doc.DocumentNode.SelectNodes("//a");

                if (links != null)
                {
                    foreach (var link in links)
                    {
                        if (_linkValidator.TryValidateInternalLink(link.GetAttributeValue("href", null), out var href) && !LinkAlreadyCrawled(href))
                        {
                            resultLinks.Add(href);
                        }
                    }
                }
            }

            return resultLinks;
        }

        private bool IsRedirect(HttpStatusCode statusCode)
        {
            return
                statusCode == HttpStatusCode.PermanentRedirect ||
                statusCode == HttpStatusCode.Redirect ||
                statusCode == HttpStatusCode.Moved;
        }
    }
}
