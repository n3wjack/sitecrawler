
# Site Crawler

A basic command-line website crawler, to crawl a site and check for any links resulting in errors, possibly for a fixed amount of time. 
It has the option of writing the crawled URL's to a CSV file at the end, including the response code.

# Features

- Crawl a site by following all links in a page. Only crawls internal links and HTML content.
- Crawls links only once.
- Possibility to export the crawled links to a CSV file, containing the referrer links (handy for tracking 404's).
- Limit crawl time to a fixed number of minutes for large sites.
- Set the number of parallel jobs to use for crawling.
- Add a delay to throttle request on slow sites, or sites with rate limits.

## Requirements

To run the application you can download the Windows x64 binary to run it without dependencies on a Windows 64-bit operating system.
If you are on another platform, you need to install the [.NET Core framework](https://dotnet.microsoft.com/download) to be able to run the general framework dependent release.

## Usage

Run the executable with `/?` to see help on the command line parameters. This will show something like the following:

	crawler.exe [--url url] [--outputfile filename] [--minutes X] [--paralleltasks X] [--requestdelay X] [--debuglogging true]

    Options:
        --url           The URL to crawl.
        --outputfile    The CSV output file to create with the crawler results (optional).
        --minutes       Time in minutes to crawl the given site (optional).
        --paralleltasks Number of tasks to use to crawl links in parallel (optional). Default is 10.
        --requestdelay  Number of milliseconds to wait after a request, to throttle request (optional). Default is 0.
        --username      Username to use when the site is using basic authentication (optional).
        --password      Password to use when the site is using basic authentication (optional).
        --useragent     A custom user agent string to use (optional).
        --debugLogging  Activate debug logging (optional).

Simple example: 

    crawler.exe --url https://awesomesite.com/ --outfile c:\temp\results.csv

## Building

You need to have the .NET Core 8.0 SDK installed to build this. You can use Visual Studio Community edition to build it.

There are 3 way to build the project.

1. Use Visual Studio.
2. Run the `build.ps1` script to build and zip the packages.
3. Use the `dotnet` tool from the command line: `dotnet build Crawler.sln`

## Testing

There's a `CrawlSite` web site project which you can run to test the basic functionality of the crawler on. It contains edge cases, bad links, and binary files.
Run the project from VS by starting with multiple startup projects, and let the crawler do it's thing on it.

It should:
- Crawl all links.
- Not get stuck in a loop.
- Not download the non-HTML files.
- Not fail on bad links, or anchor links.

