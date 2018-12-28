using System;

namespace Crawler.AppCore
{
    public class LinkValidator
    {
        private Uri _hostUri;

        public LinkValidator(Uri hostUri)
        {
            _hostUri = hostUri;
        }

        public bool TryValidateLink(string href, out string hrefout)
        {
            hrefout = null;

            if (href == null)
                return false;

            if (href.StartsWith("//") || href.StartsWith("https") || href.StartsWith("http"))
            {
                var hrefUri = new Uri(href);

                if (hrefUri.Host.Equals(_hostUri.Host))
                {
                    hrefout = href;
                    return true;
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
