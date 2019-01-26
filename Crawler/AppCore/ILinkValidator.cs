namespace Crawler.AppCore
{
    public interface ILinkValidator
    {
        bool TryValidateInternalLink(string href, out string hrefout);
    }
}