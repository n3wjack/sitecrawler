using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Crawler.AppCore
{
    public class LinkExtractor
    {
        public async Task<List<string>> ExtractLinks(string baseUrl, HttpResponseMessage response)
        {
            var linkValidator = new LinkValidator(new Uri(baseUrl));
            var resultLinks = new List<string>();

            if (IsRedirect(response.StatusCode))
            {
                var redirectUri = response.Headers.Location?.IsAbsoluteUri ?? false ? response.Headers.Location?.AbsoluteUri : null;
                if (redirectUri != null)
                {
                    if (linkValidator.TryValidateInternalLink(redirectUri, out var href))
                    {
                        resultLinks.Add(href);
                    }
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
                        if (linkValidator.TryValidateInternalLink(link.GetAttributeValue("href", null), out var href))
                        {
                            resultLinks.Add(href);
                        }
                    }
                }
            }

            return resultLinks.Distinct().ToList();
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
