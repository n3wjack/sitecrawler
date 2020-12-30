using System;

namespace Crawler.AppCore
{
    public class LinkValidator : ILinkValidator
    {
        private Uri _hostUri;

        public LinkValidator(Uri hostUri)
        {
            _hostUri = hostUri;
        }

        private string RemoveHash(string href)
        {
            if (!href.Contains('#'))
            {
                return href;
            } 
            else
            {
                return href.Split('#')[0];
            }
        }

        /// <summary>
        /// Validates a given link to check if it is a valid internal link. 
        /// If the link is external it will return false.
        /// </summary>
        /// <param name="href">The link to check.</param>
        /// <param name="hrefout">The internal link, transformed to a full URL to be able to fetch it.</param>
        /// <returns>True if the link is internal, false if external or invalid.</returns>
        public bool TryValidateInternalLink(string href, out string hrefout)
        {
            hrefout = null;

            if (href == null)
            {
                return false;
            }

            href = RemoveHash(href);

            if (href.StartsWith("//") || href.StartsWith("https:") || href.StartsWith("http:"))
            {
                if (!Uri.IsWellFormedUriString(href, UriKind.RelativeOrAbsolute))
                {
                    return false;
                }

                var hrefUri = new Uri(href);

                if (hrefUri.Host.Equals(_hostUri.Host))
                {
                    hrefout = href;
                    return true;
                }
                else 
                {
                    return false;
                }
            }

            if (Uri.TryCreate(href, UriKind.Relative, out var uri))
            {
                hrefout = new Uri(_hostUri, href).ToString();

                return true;
            }

            return false;
        }
    }
}
