using Crawler.AppCore;
using System;
using Xunit;

namespace Crawler.Tests
{
    public class LinkValidatorTest
    {
        protected string RootUrl { get; set; }

        public LinkValidatorTest(string rootUrl)
        {
            RootUrl = rootUrl;
        }

        /// <summary>
        /// Validates the given URL.
        /// </summary>
        /// <param name="url">URL to validate.</param>
        /// <param name="fullUrl">Full URL to crawl.</param>
        /// <returns>True if the URL is valid as an internal link and should be crawled.</returns>
        protected bool ValidateUrl(string url, out string fullUrl)
        {
            var sut = new LinkValidator(new Uri(RootUrl));

            return sut.TryValidateInternalLink(url, out fullUrl);
        }

        /// <summary>
        /// Validates the given URL.
        /// </summary>
        /// <param name="url">URL to validate.</param>
        /// <returns>True if the URL is valid as an internal link and should be crawled.</returns>
        protected bool ValidateUrl(string url)
        {
            var sut = new LinkValidator(new Uri("https://foobar.net"));

            return sut.TryValidateInternalLink(url, out var fullUrl);
        }
    }

    public class Given_a_root_url : LinkValidatorTest
    {
        public Given_a_root_url()
            : base("https://foobar.net")
        { }

        [Theory]
        [InlineData("https://foobar.net/foo", "https://foobar.net/foo")]
        [InlineData("//foobar.net/foo", "//foobar.net/foo")]
        [InlineData("/foo", "https://foobar.net/foo")]
        [InlineData("foo", "https://foobar.net/foo")]
        public void CheckValidUrl(string url, string expectedFullUrl)
        {
            Assert.True(ValidateUrl(url, out string fullUrl));
            Assert.Equal(expectedFullUrl, fullUrl);
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
        [InlineData("https://foobar.net/#", "https://foobar.net/")]
        [InlineData("https://foobar.net/#menu", "https://foobar.net/")]
        [InlineData("https://foobar.net/foo/#menu", "https://foobar.net/foo/")]
        public void GivenAnAnchorLinkItShouldBeValid(string url, string expectedFullUrl)
        {
            Assert.True(ValidateUrl(url, out string fullUrl));
            Assert.Equal(expectedFullUrl, fullUrl);
        }
    }

    public class Given_a_subfolder_url_with_trailing_slash : LinkValidatorTest
    {
        public Given_a_subfolder_url_with_trailing_slash()
            : base("https://foo.com/bar/")
        { }

        [Theory]
        [InlineData("/foobar", "https://foo.com/foobar")]
        [InlineData("foobar", "https://foo.com/bar/foobar")]
        public void ThenTheCorrectUrlIsReturned(string url, string expectedFullUrl)
        {
            Assert.True(ValidateUrl(url, out string fullUrl));
            Assert.Equal(expectedFullUrl, fullUrl);
        }
    }

    public class Given_a_subfolder_url_without_trailing_slash : LinkValidatorTest
    {
        public Given_a_subfolder_url_without_trailing_slash()
            : base("https://foo.com/foo/bar")
        { }

        [Theory]
        [InlineData("foobar", "https://foo.com/foo/foobar")]
        [InlineData("/foobar", "https://foo.com/foobar")]
        [InlineData("../foobar", "https://foo.com/foobar")]
        public void ThenTheCorrectUrlIsReturned(string url, string expectedFullUrl)
        {
            Assert.True(ValidateUrl(url, out string fullUrl));
            Assert.Equal(expectedFullUrl, fullUrl);
        }
    }
}
