using Crawler.AppCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Crawler.Tests
{
    public abstract class GivenAWebCrawler : WebCrawlerTest
    {
        protected WebCrawler Sut { get; set; }
        protected IList<LinkCrawlResult> Result { get; set; }

        protected WebCrawler CreateSut()
        {
            var uri = new Uri("https://foobar.com/");
            return new WebCrawler(
                new WebCrawlConfiguration { Uri = uri, RetryDelay = 1 },
                TestHttpClientFactory,
                LoggerMock.Object);
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
            
            SetupRequest("https://foobar.com/", builder.AddLink("/test", "test link").Build());
            SetupRequest("https://foobar.com/test", builder.AddLink("/test2", "").Build());
            SetupRequest("https://foobar.com/test2", builder.Build());

            Result = sut.Start();
        }

        [Fact]
        public void ThenTheTestLinkWasCrawled()
        {
            Assert.Contains(Result, r => r.Url == "https://foobar.com/test");
        }

        [Fact]
        public void ThenThereAreThreeResults()
        {
            Assert.Equal(3, Result.Count);
        }

        [Fact]
        public void ThenTheReferrerIsSetForTheTestLink()
        {
            Assert.Contains(Result, r => r.Url == "https://foobar.com/test" && r.ReferrerUrl == "https://foobar.com/");
        }
    }

    public class WhenCrawlingSiteWithAnError : GivenAWebCrawler
    {
        public WhenCrawlingSiteWithAnError()
        {
            var sut = CreateSut();
            var builder = new HttpResponseMessageBuilder();

            SetupRequest("https://foobar.com/", builder.AddLink("/test", "test link").Build());
            HttpClientMock.Setup(c => c.GetHeadersAsync("https://foobar.com/test", It.IsAny<CancellationToken>()))
                .Throws(new Exception("Oops"));

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

        [Fact]
        public void ThenTheErrorMessageIsSetForTheFailedLink()
        {
            Assert.Contains(Result, r => r.Url == "https://foobar.com/test" && r.ExceptionMessage == "Oops");
        }
    }

    public class WhenCrawlingSiteWithATaskCanceledException : GivenAWebCrawler
    {
        public WhenCrawlingSiteWithATaskCanceledException()
        {
            var sut = CreateSut();
            var builder = new HttpResponseMessageBuilder();

            SetupRequest("https://foobar.com/", builder.AddLink("/test", "test link").Build());

            HttpClientMock.Setup(c => c.GetHeadersAsync("https://foobar.com/test", It.IsAny<CancellationToken>()))
                .Throws(new TaskCanceledException("Oops"));

            Result = sut.Start();
        }

        [Fact]
        public void ThenTheTestLinkWasCrawled()
        {
            Assert.Contains(Result, r => r.Url == "https://foobar.com/");
        }

        /// <summary>
        /// When the crawl is halted and tasks are cancelled, the results aren't stored.
        /// </summary>
        [Fact]
        public void ThenThereIsOneResult()
        {
            Assert.Equal(1, Result.Count);
        }
    }

    public class WhenCrawlingSiteWithPermanentRedirect : GivenAWebCrawler
    {
        public WhenCrawlingSiteWithPermanentRedirect()
        {
            var sut = CreateSut();
            var builder = new HttpResponseMessageBuilder();

            SetupRequest("https://foobar.com/", builder.AddLink("/redirect", "redirected link").Build());
            SetupRequest("https://foobar.com/redirect", builder.MovedTo("https://foobar.com/redirect-target").Build());
            SetupRequest("https://foobar.com/redirect-target", builder.Build());

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

            SetupRequest("https://foobar.com/", builder.AddLink("/redirect", "redirected link").Build());
            SetupRequest("https://foobar.com/redirect", builder.RedirectTo("https://foobar.com/redirect-target").Build());
            SetupRequest("https://foobar.com/redirect-target", builder.Build());

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

            SetupRequest("https://foobar.com/", builder.AddLinks("/link-{0}", _links).Build());

            // Return some empty documents when one of those links is called.
            for (var i = 0; i < _links; i++)
            {
                var url = $"https://foobar.com/link-{i}";
                SetupRequest(url, builder.AddLinks("sublink-{0}", _links).Build());

                // Return an empty page for the sublinks
                for (var j = 0; j < _links; j++)
                {
                    var sublinkurl = $"https://foobar.com/sublink-{j}";
                    SetupRequest(sublinkurl, builder.Build());
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

    public class WhenCrawlingSiteWithDuplicateLinks : GivenAWebCrawler
    {
        public WhenCrawlingSiteWithDuplicateLinks()
        {
            var sut = CreateSut();
            var builder = new HttpResponseMessageBuilder();

            SetupRequest("https://foobar.com/", builder.AddLink("/test", "test link").AddLink("/test", "same link").Build());
            SetupRequest("https://foobar.com/test", builder.Build());

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

    public class WhenCrawlingSiteWithNonHtmlContent : GivenAWebCrawler
    {
        public WhenCrawlingSiteWithNonHtmlContent()
        {
            var sut = CreateSut();
            var builder = new HttpResponseMessageBuilder();

            SetupRequest("https://foobar.com/", builder.AddLink("/test", "test link").AddLink("/test", "same link").Build());
            SetupRequest("https://foobar.com/test", builder.WithContentType("application/json").AddLink("/shouldnotbecrawled", "nope").Build());

            Result = sut.Start();
        }

        [Fact]
        public void ThenTheLinkWasNotCrawled()
        {
            Assert.DoesNotContain(Result, r => r.Url == "https://foobar.com/shouldnotbecrawled");
        }

        [Fact]
        public void ThenThereIsOneResult()
        {
            Assert.Equal(2, Result.Count);
        }
    }
}
