namespace MODEL;

public class Comment
{
    public int Id { get; set; }
    public int ArticleId { get; set; }
    public string Content { get; set; }
    public int AddTime { get; set; }
    public string Ip { get; set; }
    public string UserName { get; set; }
    public string Email { get; set; }
    public int OldId { get; set; }
    public int QStatus { get; set; }
}