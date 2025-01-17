namespace MODEL;

public class Article
{
    public int AddAdminId { get; set; }
    public int AddTime { get; set; }
    public int AutoPublishTime { get; set; }
    public int CategoryId { get; set; }
    public int CommentCount { get; set; }
    public string FocusAdditionalFile { get; set; }
    public string FullDescription { get; set; }
    public string AudioPath { get; set; }
    public string VideoPath { get; set; }
    public int Id { get; set; }
    public byte IsGallery { get; set; }
    public byte IsFeatured { get; set; }
    public byte IsFocusNews { get; set; }
    public byte SurveyId { get; set; }
    public byte IsList { get; set; }
    public byte IsPinned { get; set; }
    public byte IsTop { get; set; }
    public string LatynUrl { get; set; }
    public int LikeCount { get; set; }
    public byte QStatus { get; set; }
    public string RecArticleIds { get; set; }
    public string SearchName { get; set; }
    public string ShortDescription { get; set; }
    public string ThumbnailCopyright { get; set; }
    public string ThumbnailUrl { get; set; }
    public string Title { get; set; }
    public int UpdateAdminId { get; set; }
    public int UpdateTime { get; set; }
    public int ViewCount { get; set; }
}