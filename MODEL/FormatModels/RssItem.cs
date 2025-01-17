namespace MODEL.FormatModels;

public class RssItem
{
    public string Title { get; set; }
    public string Link { get; set; }
    public string Description { get; set; }
    public DateTime PubDate { get; set; }
    public string ThumbnailUrl { get; set; }
}
