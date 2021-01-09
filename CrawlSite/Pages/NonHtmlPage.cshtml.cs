using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CrawlSite.Pages
{
    public class NonHtmlPageModel : PageModel
    {
        public void OnGet()
        {
            // Return as plain text, to see if the crawler skips it for link parsing.
            HttpContext.Response.Headers.Add("content-type", "text/plain");
        }
    }
}
