using Crawler.AppCore;
using System.Collections.Generic;
using Xunit;

namespace Crawler.Tests
{
    public class GivenALinkExtractor
    {
        protected LinkExtractor Sut { get; set; } = new LinkExtractor();
        protected string BaseUrl { get; set; } = "https://foobar.com";
        protected List<string> ResultLinks { get; set; }
    }

    public class WhenExtractingRelativeLinks : GivenALinkExtractor
    {
        public WhenExtractingRelativeLinks()
        {
            var response = new HttpResponseMessageBuilder()
                .AddLink("/relative", "relative link")
                .AddLink("anotherlink", "another relative link")
                .Build();

            ResultLinks = Sut.ExtractLinks(BaseUrl, response).Result;
        }

        [Fact]
        public void ThenTheFirstLinkIsReturned()
        {
            Assert.True(ResultLinks.Exists(s => s.Equals($"{BaseUrl}/relative")));
        }

        [Fact]
        public void ThenTheSecondLinkIsReturned()
        {
            Assert.True(ResultLinks.Exists(s => s.Equals($"{BaseUrl}/anotherlink")));
        }

        [Fact]
        public void ThenTwoLinksAreReturned()
        {
            Assert.True(ResultLinks.Count == 2);
        }
    }

    public class WhenExtractingIdenticalLinks : GivenALinkExtractor
    {
        public WhenExtractingIdenticalLinks()
        {
            var response = new HttpResponseMessageBuilder()
                .AddLink("/one", "a link")
                .AddLink("one", "the same link")
                .Build();

            ResultLinks = Sut.ExtractLinks(BaseUrl, response).Result;
        }

        [Fact]
        public void ThenLinkIsReturned()
        {
            Assert.True(ResultLinks.Exists(s => s.Equals($"{BaseUrl}/one")));
        }

        [Fact]
        public void ThenOneLinkIsReturned()
        {
            Assert.True(ResultLinks.Count == 1);
        }
    }


    public class WhenExtractingAbsoluteLinks : GivenALinkExtractor
    {
        public WhenExtractingAbsoluteLinks()
        {
            var response = new HttpResponseMessageBuilder()
                .AddLink($"{BaseUrl}/somepage", "a page")
                .AddLink("https://www.external.com/", "an external site link")
                .Build();

            ResultLinks = Sut.ExtractLinks(BaseUrl, response).Result;
        }

        [Fact]
        public void ThenTheCorrectLinkIsReturned()
        {
            Assert.True(ResultLinks.Exists(s => s.Equals($"{BaseUrl}/somepage")));
        }

        [Fact]
        public void ThenOnlyOneLinkIsReturned()
        {
            Assert.True(ResultLinks.Count == 1);
        }
    }

    public class WhenExtractingFromRedirect : GivenALinkExtractor
    {
        public WhenExtractingFromRedirect()
        {
            var response = new HttpResponseMessageBuilder()
                .RedirectTo($"{BaseUrl}/login")
                .Build();

            ResultLinks = Sut.ExtractLinks(BaseUrl, response).Result;
        }

        [Fact]
        public void ThenOneLinkIsReturned()
        {
            Assert.True(ResultLinks.Count == 1);
        }

        [Fact]
        public void ThenTheCorrectLinkIsReturned()
        {
            Assert.True(ResultLinks.Exists(s => s.Equals($"{BaseUrl}/login")));
        }
    }

    public class WhenExtractingFromRedirectToExternalSite : GivenALinkExtractor
    {
        public WhenExtractingFromRedirectToExternalSite()
        {
            var response = new HttpResponseMessageBuilder()
                .RedirectTo($"https://www.external.com/foo")
                .Build();

            ResultLinks = Sut.ExtractLinks(BaseUrl, response).Result;
        }

        [Fact]
        public void ThenNoLinksAreReturned()
        {
            Assert.True(ResultLinks.Count == 0);
        }
    }
}
