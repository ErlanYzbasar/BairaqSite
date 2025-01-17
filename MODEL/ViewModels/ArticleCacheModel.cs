namespace MODEL.ViewModels;

public class ArticleCacheModel
{
    public Article Article { get; init; }
    public Article LastArticle { get; init; }
    public Article NextArticle { get; init; }
    public Rating Rating { get; init; }
    public  List<Article> RecArticleList { get; init; }
    public List<Tag> TagList { get; init; }
}