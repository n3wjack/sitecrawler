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
        private ConcurrentQueue<LinkToCrawl> _linksToCrawl = new ConcurrentQueue<LinkToCrawl>();
        private Task[] _crawlTasks;
        private readonly WebCrawlConfiguration _configuration;
        private readonly Func<IHttpClient> _httpClientFactory;
        private ParallelOptions _parallelOptions;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isFirstLink;

        public event EventHandler<LinkCrawlResult> LinkCrawled;

        ~WebCrawler()
        {
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Dispose();
            }
        }

        public WebCrawler(WebCrawlConfiguration configuration, Func<IHttpClient> httpClientFactory)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
        }

        public IList<LinkCrawlResult> Start()
        {
            SetOptions();

            var startUri = _configuration.Uri.ToString();

            _isFirstLink = true;
            _linksToCrawl.Enqueue(new LinkToCrawl { Url = startUri, Referrer = string.Empty });

            _crawlTasks = Enumerable.Range(1, 10).Select(i => new Task(CrawlTaskAction, _cancellationTokenSource.Token)).ToArray();
            _crawlTasks.ToList().ForEach(t => t.Start());

            try
            {
                Task.WaitAll(_crawlTasks);
                Console.WriteLine("**** Finished crawling ****");
            }
            catch (AggregateException ex)
            {
                Console.WriteLine("\n***** Exceptions ****\n");
                ex.InnerExceptions.ToList().ForEach(e => Console.WriteLine(ex.Message));
            }

            return _linkCrawlResults.Values.ToList();
        }

        private void CrawlTaskAction()
        {
            LinkToCrawl linkToCrawl;
            int failedDequeues = 0;

            do
            {
                if (_linksToCrawl.TryDequeue(out linkToCrawl))
                {
                    failedDequeues = 0;
                    var linksToCrawl = CrawlLink(linkToCrawl.Url, linkToCrawl.Referrer);
                    linksToCrawl.ForEach(l => _linksToCrawl.Enqueue(l));
                    _isFirstLink = false;
                }
                else
                {
                    if (!_isFirstLink)
                    {
                        failedDequeues++;
                    }
                    // Wait a bit to make sure other tasks processing links have the change to add new links to the queue.
                    Console.WriteLine("=== Task waiting...");
                    Thread.Sleep(1000);
                }
            } while (failedDequeues <= 3 && !_cancellationTokenSource.Token.IsCancellationRequested);

            Console.WriteLine("=== TASK STOPPED ===");
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

        private bool LinkAlreadyCrawled(string link)
        {
            return _linkCrawlResults.ContainsKey(link);
        }

        private void AddCrawlResult(LinkCrawlResult crawlResult)
        {
            LinkCrawled?.Invoke(this, crawlResult);
            // AddOrUpdate because we added an empty result earlier to block the URL from being crawled by another thread.
            _linkCrawlResults.AddOrUpdate(crawlResult.Url, crawlResult, (key, value) => crawlResult);
        }

        /// <summary>
        /// Crawls a given link and returns a list of new found links to crawl.
        /// </summary>
        /// <param name="url">The URL of the link to crawl.</param>
        /// <param name="referrerUrl">The referrer of the link.</param>
        /// <returns>A list of links found.</returns>
        private List<LinkToCrawl> CrawlLink(string url, string referrerUrl)
        {
            var crawlResult = new LinkCrawlResult { Url = url, ReferrerUrl = referrerUrl };

            // Add an empty crawlresult to avoid other threads from also crawling the same url.
            if (!_linkCrawlResults.TryAdd(url, crawlResult))
            {
                Console.WriteLine("=== Already crawled " + url);
                return new List<LinkToCrawl>();
            }

            if (url.StartsWith("/"))
            {
                url = $"{_configuration.Uri.Scheme}://{_configuration.Uri.Host}{url}";
            }

            using (var client = _httpClientFactory())
            {
                try
                {
                    var response = client.GetAsync(url, _cancellationTokenSource.Token).Result;
                    var links = ExtractLinks(url, response).Result.Distinct().ToList();

                    crawlResult.Links = links;
                    crawlResult.StatusCode = response.StatusCode;
                    AddCrawlResult(crawlResult);

                    return links.Select(link => new LinkToCrawl { Url = link, Referrer = referrerUrl }).ToList();
                }
                catch (AggregateException aggregateException)
                {
                    aggregateException.Handle(ex =>
                    {
                        if (ex is TaskCanceledException)
                            Console.WriteLine("Cancelled : " + ex.Message);
                        return ex is TaskCanceledException;
                    });

                    return new List<LinkToCrawl>();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"-- CRAWL ERROR on {url} : {ex.Message}");
                    throw;
                }
            }
        }

        private async Task<List<string>> ExtractLinks(string url, HttpResponseMessage response)
        {
            var resultLinks = new List<string>();

            if (IsRedirect(response.StatusCode))
            {
                var redirectUri = response.Headers.Location?.IsAbsoluteUri ?? false ? response.Headers.Location?.AbsoluteUri : null;
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
                        var linkValidator = new LinkValidator(new Uri(url));
                        if (linkValidator.TryValidateInternalLink(link.GetAttributeValue("href", null), out var href) 
                            && !LinkAlreadyCrawled(href))
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
