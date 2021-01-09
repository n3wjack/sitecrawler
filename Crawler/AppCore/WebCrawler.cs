using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Crawler.AppCore
{
    public class WebCrawler
    {
        private ConcurrentDictionary<string, LinkCrawlResult> _linkCrawlResults = new ConcurrentDictionary<string, LinkCrawlResult>();
        private ConcurrentQueue<LinkToCrawl> _linksToCrawl = new ConcurrentQueue<LinkToCrawl>();
        private Task[] _crawlTasks;
        private LinkExtractor _linkExtractor = new LinkExtractor();
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
            _linksToCrawl.Enqueue(new LinkToCrawl { Url = startUri, Referrer = startUri });

            _crawlTasks = Enumerable.Range(1, _configuration.ParallelTasks).Select((n) => CrawlTaskAction(n)).ToArray();

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

        private async Task CrawlTaskAction(int taskNumber)
        {
            LinkToCrawl linkToCrawl;
            int failedDequeues = 0;

            Console.WriteLine($"=== TASK {taskNumber} started");
            do
            {
                if (_linksToCrawl.TryDequeue(out linkToCrawl))
                {
                    failedDequeues = 0;
                    if (!LinkAlreadyCrawled(linkToCrawl.Url))
                    {
                        Console.WriteLine($"=== Start crawling link {linkToCrawl.Url}");
                        var crawlResult = await CrawlLink(linkToCrawl.Url, linkToCrawl.Referrer);

                        if (crawlResult != null)
                        {
                            AddCrawlResult(crawlResult);
                            Console.WriteLine($"=== Ended crawling link {linkToCrawl.Url}, found {crawlResult.Links.Count} links");
                            crawlResult.Links
                                .Select(l => new LinkToCrawl { Referrer = crawlResult.Url, Url = l })
                                .ToList()
                                .ForEach(l => _linksToCrawl.Enqueue(l));
                        }
                        else
                        {
                            Console.WriteLine($"=== Failed to crawl link {linkToCrawl.Url}.");
                        }

                        _isFirstLink = false;

                        await Task.Delay(_configuration.RequestWaitDelay);
                    }
                    else
                    {
                        Console.WriteLine($"=== Already crawling link {linkToCrawl.Url}, skipping...");
                    }
                }
                else
                {
                    if (!_isFirstLink)
                    {
                        failedDequeues++;
                        Console.WriteLine($"=== Task {taskNumber} waiting...");
                    }

                    // Wait a bit to make sure other tasks processing links have the change to add new links to the queue.
                    await Task.Delay(_configuration.RetryDelay);
                }
            } while (failedDequeues <= 3 && !_cancellationTokenSource.Token.IsCancellationRequested);

            Console.WriteLine($"=== TASK {taskNumber} STOPPED ===");
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
        /// Crawls a given link and returns the crawl result.
        /// </summary>
        /// <param name="url">The URL of the link to crawl.</param>
        /// <param name="referrerUrl">The referrer of the link.</param>
        /// <returns>The crawl results, or null if the URL could not be crawled or was already crawled.</returns>
        private async Task<LinkCrawlResult> CrawlLink(string url, string referrerUrl)
        {
            var crawlResult = new LinkCrawlResult { Url = url, ReferrerUrl = referrerUrl };

            // Add an empty crawlresult to avoid other threads from also crawling the same url.
            if (!_linkCrawlResults.TryAdd(url, crawlResult))
            {
                Console.WriteLine("=== Already crawled " + url);
                return null;
            }

            if (url.StartsWith("/"))
            {
                url = $"{_configuration.Uri.Scheme}://{_configuration.Uri.Host}{url}";
            }

            using (var client = _httpClientFactory())
            {
                try
                {
                    var response = await client.GetAsync(url, _cancellationTokenSource.Token);
                    var links = (await _linkExtractor.ExtractLinks(url, response))
                        .Where(l => !LinkAlreadyCrawled(l))
                        .ToList();

                    crawlResult.Links = links;
                    crawlResult.StatusCode = response.StatusCode;

                    return crawlResult;
                }
                catch (AggregateException aggregateException)
                {
                    aggregateException.Handle(ex =>
                    {
                        if (ex is TaskCanceledException)
                            Console.WriteLine("Cancelled : " + ex.Message);
                        return ex is TaskCanceledException;
                    });

                    _linkCrawlResults.TryRemove(url, out crawlResult);
                    return null;
                }
                catch (TaskCanceledException tce)
                {
                    Console.WriteLine("Cancelled : " + tce.Message);

                    _linkCrawlResults.TryRemove(url, out crawlResult);
                    return null;
                }
                catch (Exception ex)
                {
                    crawlResult.RequestFailed = true;
                    crawlResult.ExceptionMessage = ex.Message;
                    Console.WriteLine($"-- CRAWL ERROR on {url} : {ex.Message}");

                    return crawlResult;
                }
            }
        }
    }
}
