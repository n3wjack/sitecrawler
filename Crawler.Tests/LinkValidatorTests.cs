using Crawler.AppCore;
using System;
using Xunit;

namespace Crawler.Tests
{
    public class LinkValidatorTests
    {
        [Theory]
        [InlineData("https://foobar.net/foo")]
        [InlineData("//foobar.net/foo")]
        [InlineData("/foo")]
        [InlineData("foo")]
        public void CheckValidUrl(string url)
        {
            var sut = new LinkValidator(new Uri("https://foobar.net"));

            Assert.True(sut.TryValidateInternalLink(url, out string href));
        }

        [Theory]
        [InlineData("https://google.com")]
        [InlineData("ftp://foobar.net/bar")]
        public void CheckExternalUrl(string url)
        {
            var sut = new LinkValidator(new Uri("https://foobar.net"));

            Assert.False(sut.TryValidateInternalLink(url, out string href));
        }

        [Theory]
        [InlineData("https://?google.com")]
        [InlineData("https://?foobar.net/foo")]
        public void CheckInvalidUrl(string url)
        {
            var sut = new LinkValidator(new Uri("https://foobar.net"));

            Assert.False(sut.TryValidateInternalLink(url, out string href));
        }
    }
}
