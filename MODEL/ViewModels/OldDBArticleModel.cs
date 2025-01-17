namespace MODEL.ViewModels;

public class OldDbArticleModel
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public List<Tag> TagList { get; set; }
    public int AddTime { get; set; }
    public int UpdateTime { get; set; }
    public string FullDescription { get; set; }
    public string ShortDescription { get; set; }
    public string ThumbnailUrl { get; set; }
    public string ThumbnailCopyright { get; set; }
    public string Title { get; set; }
    public string LatynUrl { get; set; }
    public string OldLatynUrl { get; set; }
    public string Author { get; set; }
    public int ViewCount { get; set; }
}