using Crawler.AppCore;
using Crawler.Configuration;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Crawler
{
    class Program
    {
        private static int _linksCrawled;
        private static DateTime _startTime;

        static void Main(string[] args)
        {
            var appSettings = new AppSettings(args);
            new ConfigurationBuilder().AddCommandLine(args).Build().Bind(appSettings);

            if (!appSettings.IsValid || appSettings.ShowHelp)
            {
                if (!appSettings.ShowHelp)
                {
                    Console.WriteLine("\nERROR : Invalid or incomplete command line parameters.");
                }

                ShowHelp();
                return;
            }

            Console.WriteLine($"Ready to crawl {appSettings.Url}");

            Console.WriteLine("Press ENTER to start crawling.");
            Console.ReadLine();

            var crawler = new WebCrawler(new WebCrawlConfiguration { Uri = new Uri(appSettings.Url) });
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
            if (!string.IsNullOrWhiteSpace(appSettings.OutputFile))
            {
                WriteCsv(results, appSettings.OutputFile);
            }

            Console.ReadLine();
        }

        private static void ShowHelp()
        {
            var assembly = typeof(Program).GetTypeInfo().Assembly;
            using (var s = new StreamReader(assembly.GetManifestResourceStream("Crawler.Help.txt")))
            {
                Console.Write(s.ReadToEnd());
            }
        }

        private static void Crawler_LinkCrawled(object sender, LinkCrawlResult crawlResult)
        {
            _linksCrawled++;

            var duration = DateTime.UtcNow - _startTime;
            var linksPerSecond = duration.TotalSeconds == 0 ? 0 : _linksCrawled / duration.TotalSeconds;

            Console.WriteLine($"-- Crawled: {crawlResult.Url}\n\t Result : {crawlResult.StatusCode}, Links: {crawlResult.Links.Count} - Total crawled: {_linksCrawled} in {duration} ({linksPerSecond} links/s) ");
        }

        private static void WriteCsv(IList<LinkCrawlResult> results, string filename)
        {
            Console.WriteLine("Writing to csv file");
            Console.WriteLine($"   Results : {results.Count}");
            results.ToList().ForEach(r => System.IO.File.AppendAllText(filename, $"\"{r.Url}\";{r.StatusCode}\n"));
        }
    }
}
