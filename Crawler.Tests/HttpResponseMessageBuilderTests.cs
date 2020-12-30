using System.Net;
using Xunit;

namespace Crawler.Tests
{
    /// <summary>
    /// Test to test HttpResponseMessageBuilder testcode for tests. Lot's of testing isn't it?
    /// </summary>
    public class HttpResponseMessageBuilderTests
    {
        [Fact]
        public async void WhenAddLinkIsUsed_TheLinkIsInTheResult()
        {
            var r = new HttpResponseMessageBuilder().AddLink("foo", "foo link").Build();

            var s = await r.Content.ReadAsStringAsync();

            Assert.Contains("<a href=\"foo\">", s);
        }

        [Fact]
        public void WhenCallingBuildTwiceStatusCodeIsReset()
        {
            var r = new HttpResponseMessageBuilder().MovedTo("foo").Build();
            r = new HttpResponseMessageBuilder().Build();

            Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        }

        [Fact]
        public void WhenMovedToIsCalledStatusCodeIsCorrect()
        {
            var r = new HttpResponseMessageBuilder().MovedTo("foo").Build();

            Assert.Equal(HttpStatusCode.Moved, r.StatusCode);
        }

        [Fact]
        public void WhenRedirectToIsCalledStatusCodeIsCorrect()
        {
            var r = new HttpResponseMessageBuilder().RedirectTo("foo").Build();

            Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
        }
    }
}
