using System.Net;
using Xunit;

namespace Crawler.Tests
{
    /// <summary>
    /// Test to test HttpContentBuilder test code for tests. Lot's of testing isn't it?
    /// </summary>
    public class HttpContentBuilderTests
    {
        [Fact]
        public async void WhenAddLinkIsUsed_TheLinkIsInTheResult()
        {
            var r = new HttpResponseMessageBuilder().AddLink("foo", "foo link").Build();

            var s = await r.Content.ReadAsStringAsync();

            Assert.Contains("<a href=\"foo\">", s);
        }

        [Fact]
        public async void WhenCallingBuildTwiceStatusCodeIsReset()
        {
            var r = new HttpResponseMessageBuilder().RedirectsTo("foo").Build();
            r = new HttpResponseMessageBuilder().Build();

            Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        }

        [Fact]
        public async void WhenRedirectingStatusCodeIsCorrect()
        {
            var r = new HttpResponseMessageBuilder().RedirectsTo("foo").Build();

            Assert.Equal(HttpStatusCode.Moved, r.StatusCode);
        }
    }
}
