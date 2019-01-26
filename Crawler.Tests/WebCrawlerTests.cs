using Crawler.AppCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Crawler.Tests
{
    public abstract class GivenAWebCrawler
    {
        protected Mock<IHttpClient> HttpClientMock { get; set; } = new Mock<IHttpClient>();
        protected WebCrawler Sut { get; set; }
        protected IList<LinkCrawlResult> Result { get; set; }

        protected WebCrawler CreateSut()
        {
            var uri = new Uri("https://foobar.com/");
            return new WebCrawler(
                new WebCrawlConfiguration { Uri = uri },
                testHttpClientFactory,
                new LinkValidator(uri));
        }

        private IHttpClient testHttpClientFactory()
        {
            return HttpClientMock.Object;
        }
    }

    public class WhenCrawlingSiteWithLink : GivenAWebCrawler
    {
        public WhenCrawlingSiteWithLink()
        {
            var sut = CreateSut();
            var builder = new HttpResponseMessageBuilder();

            HttpClientMock.Setup(c => c.GetAsync("https://foobar.com/", It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(builder.AddLink("/test", "test link").Build()));

            HttpClientMock.Setup(c => c.GetAsync("https://foobar.com/test", It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(builder.Build()));

            Result = sut.Start();
        }

        [Fact]
        public void ThenTheTestLinkWasCrawled()
        {
            Assert.Contains(Result, r => r.Url == "https://foobar.com/test");
        }

        [Fact]
        public void ThenThereAreTwoResults()
        {
            Assert.Equal(2, Result.Count);
        }
    }

    public class WhenCrawlingSiteWithPermanentRedirect : GivenAWebCrawler
    {
        public WhenCrawlingSiteWithPermanentRedirect()
        {
            var sut = CreateSut();
            var builder = new HttpResponseMessageBuilder();

            HttpClientMock.Setup(c => c.GetAsync("https://foobar.com/", It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(builder.AddLink("/redirect", "redirected link").Build()));

            HttpClientMock.Setup(c => c.GetAsync("https://foobar.com/redirect", It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(builder.MovedTo("https://foobar.com/redirect-target").Build()));

            HttpClientMock.Setup(c => c.GetAsync("https://foobar.com/redirect-target", It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(builder.Build()));

            Result = sut.Start();
        }

        [Fact]
        public void ThenTheRedirectIsFollowed()
        {
            Assert.Contains(Result, r => r.Url == "https://foobar.com/redirect-target");
        }

        [Fact]
        public void ThenThereAreThreeResults()
        {
            Assert.Equal(3, Result.Count);
        }
    }

    public class WhenCrawlingSiteWithRedirect : GivenAWebCrawler
    {
        public WhenCrawlingSiteWithRedirect()
        {
            var sut = CreateSut();
            var builder = new HttpResponseMessageBuilder();

            HttpClientMock.Setup(c => c.GetAsync("https://foobar.com/", It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(builder.AddLink("/redirect", "redirected link").Build()));

            HttpClientMock.Setup(c => c.GetAsync("https://foobar.com/redirect", It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(builder.RedirectTo("https://foobar.com/redirect-target").Build()));

            HttpClientMock.Setup(c => c.GetAsync("https://foobar.com/redirect-target", It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(builder.Build()));

            Result = sut.Start();
        }

        [Fact]
        public void ThenTheRedirectIsFollowed()
        {
            Assert.Contains(Result, r => r.Url == "https://foobar.com/redirect-target");
        }

        [Fact]
        public void ThenThereAreThreeResults()
        {
            Assert.Equal(3, Result.Count);
        }
    }
}
