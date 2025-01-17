namespace MODEL;

public class Question
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Type { get; set; }
    public int AddAdminId { get; set; }
    public int UpdateAdminId { get; set; }
    public string Language { get; set; }
    public int AddTime { get; set; }
    public int UpdateTime { get; set; }
    public byte QStatus { get; set; }
}