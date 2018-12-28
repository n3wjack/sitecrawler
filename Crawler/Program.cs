using Crawler.AppCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Crawler
{
    class Program
    {
        private static int _linksCrawled;
        private static DateTime _startTime;

        static void Main(string[] args)
        {
            Console.WriteLine("Press ENTER to start crawling.");
            Console.ReadLine();

            //var uri = "https://previewm.janitv.be/";
            var uri = "https://localhost:44339/";

            var crawler = new WebCrawler(new WebCrawlConfiguration { Uri = new Uri(uri) });
            crawler.LinkCrawled += Crawler_LinkCrawled;

            Console.CancelKeyPress += (sender, e) => 
            {
                e.Cancel = true;
                Console.WriteLine("*** Stopping crawler **** ");
                crawler.Stop();
                Console.WriteLine("*** Crawler stop signaled **** ");
            };

            _startTime = DateTime.UtcNow;
            var results = crawler.Start();

            Console.WriteLine("Crawling done.");
            WriteCsv(results);

            Console.ReadLine();
        }

        private static void Crawler_LinkCrawled(object sender, LinkCrawlResult crawlResult)
        {
            _linksCrawled++;

            var duration = DateTime.UtcNow - _startTime;
            var linksPerSecond = duration.TotalSeconds == 0 ? 0 : _linksCrawled / duration.TotalSeconds;

            Console.WriteLine($"-- Crawled: {crawlResult.Url}\n\t Result : {crawlResult.StatusCode}, Links: {crawlResult.Links.Count} - Total crawled: {_linksCrawled} in {duration} ({linksPerSecond} links/s) ");
        }

        private static void WriteCsv(IList<LinkCrawlResult> results)
        {
            var filename = @".\crawl-result.csv";

            Console.WriteLine("Writing to csv file");
            Console.WriteLine($"   Results : {results.Count}");
            results.ToList().ForEach(r => System.IO.File.AppendAllText(filename, $"\"{r.Url}\";{r.StatusCode}\n"));
        }
    }
}
