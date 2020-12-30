using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Linq;

namespace Crawler.Tests
{
    public class HttpResponseMessageBuilder
    {
        private static class Headers
        {
            public const string Location = "Location";
        }

        private StringBuilder _sb = new StringBuilder();
        private Dictionary<string, string> _headers = new Dictionary<string, string>();
        private HttpStatusCode _statusCode;
        private bool _writeBody;

        public HttpResponseMessageBuilder()
        {
            Initialize();
        }

        public HttpResponseMessageBuilder AddLink(string url, string text)
        {
            _sb.Append($"<a href=\"{url}\">{text}</a>");

            return this;
        }

        public HttpResponseMessageBuilder AddLinks(string linkFormat, int links)
        {
            for (var i = 0; i < links; i++)
            {
                var link = string.Format(linkFormat, i);
                AddLink(link, link);
            }

            return this;
        }

        public HttpResponseMessage Build()
        {
            if (_writeBody)
            {
                _sb.Append("</body></html>");
            }
            else
            {
                _sb.Clear();
            }

            var httpResponse = new HttpResponseMessage(_statusCode)
            {
                Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(_sb.ToString())))
            };

            _headers.ToList().ForEach(kv => httpResponse.Headers.Add(kv.Key, kv.Value));
            
            Initialize();

            return httpResponse;
        }

        public HttpResponseMessageBuilder MovedTo(string redirectUrl)
        {
            _statusCode = HttpStatusCode.Moved;
            _writeBody = false;

            WithHeader(Headers.Location, redirectUrl);

            return this;
        }

        public HttpResponseMessageBuilder RedirectTo(string redirectUrl)
        {
            _statusCode = HttpStatusCode.Redirect;
            _writeBody = false;

            WithHeader(Headers.Location, redirectUrl);

            return this;
        }

        public HttpResponseMessageBuilder WithHeader(string name, string value)
        {
            _headers.Add(name, value);

            return this;
        }

        private void Initialize()
        {
            _sb.Clear();
            _sb.Append("<html><body>");
            _headers.Clear();
            _statusCode = HttpStatusCode.OK;
            _writeBody = true;
        }
    }
}
