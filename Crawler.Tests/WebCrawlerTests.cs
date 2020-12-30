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
                new WebCrawlConfiguration { Uri = uri, RetryDelay = 1 },
                TestHttpClientFactory);
        }

        private IHttpClient TestHttpClientFactory()
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
                .Returns(Task.FromResult(builder.AddLink("/test2", "").Build()));

            HttpClientMock.Setup(c => c.GetAsync("https://foobar.com/test2", It.IsAny<CancellationToken>()))
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
            Assert.Equal(3, Result.Count);
        }

        [Fact]
        public void ThenTheReferrerIsSetForTheTestLink()
        {
            Assert.Contains(Result, r => r.Url == "https://foobar.com/test" && r.ReferrerUrl == "https://foobar.com/");
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

    public class WhenCrawlingSiteWithLotsOfLinks : GivenAWebCrawler
    {
        private WebCrawler _sut;
        private const int _links = 42;

        public WhenCrawlingSiteWithLotsOfLinks()
        {
            _sut = CreateSut();

            var builder = new HttpResponseMessageBuilder();

            HttpClientMock.Setup(c => c.GetAsync("https://foobar.com/", It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(builder.AddLinks("/link-{0}", _links).Build()));

            // Return some empty documents when one of those links is called.
            for (var i = 0; i < _links; i++)
            {
                var url = $"https://foobar.com/link-{i}";

                HttpClientMock.Setup(c => c.GetAsync(url, It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(builder.AddLinks("sublink-{0}", _links).Build()));

                // Return an empty page for the sublinks
                for (var j = 0; j < _links; j++)
                {
                    var sublinkurl = $"https://foobar.com/sublink-{j}";

                    HttpClientMock.Setup(c => c.GetAsync(sublinkurl, It.IsAny<CancellationToken>()))
                        .Returns(Task.FromResult(builder.Build()));
                }
            }
        }

        [Fact]
        public void ThenAllLinksAreCrawled()
        {
            var result = _sut.Start();

            Assert.Equal((_links * 2) + 1, result.Count);
        }
    }
}
