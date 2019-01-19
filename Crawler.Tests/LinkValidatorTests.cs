using Crawler.AppCore;
using System;
using Xunit;

namespace Crawler.Tests
{
    public class LinkValidatorTests
    {
        /// <summary>
        /// Validates the given URL.
        /// </summary>
        /// <param name="url">URL to validate.</param>
        /// <returns>True if the URL is valid as an internal link and should be crawled.</returns>
        private bool ValidateUrl(string url)
        {
            var sut = new LinkValidator(new Uri("https://foobar.net"));

            return sut.TryValidateInternalLink(url, out string href);
        }

        [Theory]
        [InlineData("https://foobar.net/foo")]
        [InlineData("//foobar.net/foo")]
        [InlineData("/foo")]
        [InlineData("foo")]
        public void CheckValidUrl(string url)
        {
            Assert.True(ValidateUrl(url));
        }

        [Theory]
        [InlineData("https://google.com")]
        [InlineData("ftp://foobar.net/bar")]
        public void CheckExternalUrl(string url)
        {
            Assert.False(ValidateUrl(url));
        }

        [Theory]
        [InlineData("https://?google.com")]
        [InlineData("https://?foobar.net/foo")]
        public void CheckInvalidUrl(string url)
        {
            Assert.False(ValidateUrl(url));
        }

        [Theory]
        [InlineData("https://foobar.net/#")]
        [InlineData("https://foobar.net/#menu")]
        [InlineData("https://foobar.net/foo/#menu")]
        public void GivenAnAnchorLinkItShouldBeInvalid(string url)
        {
            Assert.False(ValidateUrl(url));
        }
    }
}
