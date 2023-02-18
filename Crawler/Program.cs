using Crawler.AppCore;
using Crawler.Configuration;
using Crawler.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;

namespace Crawler
{
    class Program
    {
        private static int _linksCrawled;
        private static DateTime _startTime;
        private static ILogger _logger;

        static void Main(string[] args)
        {
            var appSettings = new AppSettings(args);
            new ConfigurationBuilder().AddCommandLine(args).Build().Bind(appSettings);
            
            _logger = CreateLogger(appSettings.DebugLogging);

            if (!appSettings.IsValid || appSettings.ShowHelp)
            {
                if (!appSettings.ShowHelp)
                {
                    Console.WriteLine("\nERROR : Invalid or incomplete command line parameters.");
                }

                ShowHelp();
                return;
            }

            var crawler = WebCrawlerFactory.Create(new WebCrawlConfiguration 
            { 
                Uri = new Uri(appSettings.Url),
                RequestWaitDelay = appSettings.RequestDelay,
                ParallelTasks = appSettings.ParallelTasks,
                Username = appSettings.Username,
                Password = appSettings.Password
            }, _logger);
            crawler.LinkCrawled += Crawler_LinkCrawled;

            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                StopCrawler(crawler);
            };

            if (appSettings.Minutes != 0)
            {
                _logger.LogInformation($"Stopping crawling after {appSettings.Minutes} minutes.");
                var timer = new Timer(TimerCallback, crawler, appSettings.Minutes * 60 * 1000, Timeout.Infinite);
            }

            _startTime = DateTime.UtcNow;
            var results = crawler.Start();

            _logger.LogInformation("Crawling done.");
            if (!string.IsNullOrWhiteSpace(appSettings.OutputFile))
            {
                WriteCsv(results, appSettings.OutputFile);
            }
        }

        private static ILogger CreateLogger(bool debugLogging)
        {
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .AddFilter("Microsoft", LogLevel.Warning)
                    .AddFilter("System", LogLevel.Warning)
                    .AddFilter("Crawler", debugLogging ? LogLevel.Debug : LogLevel.Information)
                    .AddConsole();
            });
            ILogger logger = loggerFactory.CreateLogger<Program>();
            
            return logger;
        }

        private static void TimerCallback(object state)
        {
            StopCrawler(state as WebCrawler);
        }

        private static void StopCrawler(WebCrawler crawler)
        {
            _logger.LogInformation("*** Stopping crawler **** ", ConsoleColor.Yellow);
            crawler.Stop();
            _logger.LogInformation("*** Crawler stop signaled **** ", ConsoleColor.Yellow);
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

            _logger.LogInformation(
                $"-- Crawled: {crawlResult.Url}\n\t Result : {crawlResult.StatusCode}, Links: {crawlResult.Links.Count} - Total crawled: {_linksCrawled} in {duration} ({linksPerSecond} links/s) ",
                GetColorForStatusCode(crawlResult.StatusCode));
        }

        private static ConsoleColor GetColorForStatusCode(HttpStatusCode statusCode)
        {
            switch (statusCode)
            {
                case HttpStatusCode.OK: 
                    return ConsoleColor.Green;
                case HttpStatusCode.InternalServerError:
                case HttpStatusCode.BadRequest:
                    return ConsoleColor.Red;
                default: 
                    return ConsoleColor.Yellow;
            }
        }

        private static void WriteCsv(IList<LinkCrawlResult> results, string filename)
        {
            _logger.LogInformation("Writing to csv file");
            _logger.LogInformation($"   Results : {results.Count}");

            if (File.Exists(filename))
            {
                File.Delete(filename);
            }

            // Add the header.
            File.AppendAllText(filename, "StatusCode,Url,ReferrerUrl,ExceptionMessage\n");

            results.ToList().ForEach(r => 
                File.AppendAllText(filename, $"{r.StatusCode},\"{r.Url}\",\"{r.ReferrerUrl}\",\"{r.ExceptionMessage}\"\n"));

            _logger.LogInformation("Done writing CSV file.");
        }
    }
}
