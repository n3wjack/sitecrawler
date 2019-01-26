﻿using Crawler.AppCore;
using Crawler.Configuration;
using Crawler.Infrastructure;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

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

            var crawler = WebCrawlerFactory.Create(new WebCrawlConfiguration { Uri = new Uri(appSettings.Url) });
            crawler.LinkCrawled += Crawler_LinkCrawled;

            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                StopCrawler(crawler);
            };

            if (appSettings.Minutes != 0)
            {
                Console.WriteLine($"Stopping crawling after {appSettings.Minutes} minutes.");
                var timer = new Timer(TimerCallback, crawler, appSettings.Minutes * 60 * 1000, Timeout.Infinite);
            }

            _startTime = DateTime.UtcNow;
            var results = crawler.Start();

            Console.WriteLine("Crawling done.");
            if (!string.IsNullOrWhiteSpace(appSettings.OutputFile))
            {
                WriteCsv(results, appSettings.OutputFile);
            }

            Console.ReadLine();
        }

        private static void TimerCallback(object state)
        {
            StopCrawler(state as WebCrawler);
        }

        private static void StopCrawler(WebCrawler crawler)
        {
            var c = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("*** Stopping crawler **** ");
            crawler.Stop();
            Console.WriteLine("*** Crawler stop signaled **** ");
            Console.ForegroundColor = c;
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
            results.ToList().ForEach(r => File.AppendAllText(filename, $"\"{r.Url}\";{r.StatusCode}\n"));
        }
    }
}
