using System.Xml.Linq;
using MODEL;
using MODEL.FormatModels;

namespace COMMON;

public static class RssHelper
{
    public static string GenerateRss(List<RssItem> rssItemList, Sitesetting sitesetting, string language)
    {
        var siteUrl = QarSingleton.GetInstance().GetSiteUrl();
        var currentDateTime = DateTime.UtcNow;
        XNamespace dc = "http://purl.org/dc/elements/1.1/";
        XNamespace content = "http://purl.org/rss/1.0/modules/content/";
        XNamespace atom = "http://www.w3.org/2005/Atom";
        XNamespace media = "http://search.yahoo.com/mrss/";

        var rss = new XDocument(
            new XElement("rss",
                new XAttribute(XNamespace.Xmlns + "dc", dc),
                new XAttribute(XNamespace.Xmlns + "content", content),
                new XAttribute(XNamespace.Xmlns + "atom", atom),
                new XAttribute(XNamespace.Xmlns + "media", media),
                new XAttribute("version", "2.0"),
                new XElement("channel",
                    new XElement("title", sitesetting.Title),
                    new XElement("link", siteUrl),
                    new XElement("description", sitesetting.Description),
                    new XElement("language", language),
                    new XElement("image", PathHelper.Combine(siteUrl, sitesetting.DarkLogo)),
                    new XElement("pubDate", currentDateTime.ToString("R")),
                    new XElement("lastBuildDate", currentDateTime.ToString("R")),
                    new XElement(atom + "link",
                        new XAttribute("href", PathHelper.Combine(siteUrl, language, "xml/rss.xml")),
                        new XAttribute("rel", "self"),
                        new XAttribute("type", "application/rss+xml")
                    ),
                    rssItemList.Select(item =>
                        new XElement("item",
                            new XElement("title", item.Title),
                            new XElement("link", item.Link),
                            new XElement("description", item.Description),
                            new XElement("pubDate", item.PubDate.ToString("R")),
                            new XElement(media + "thumbnail",
                                new XAttribute("width", 300),
                                new XAttribute("height", 168),
                                new XAttribute("url", PathHelper.Combine(siteUrl, item.ThumbnailUrl))
                            )
                        )
                    )
                )
            )
        );

        return rss.ToString();
    }
}