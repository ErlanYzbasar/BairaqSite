namespace MODEL;

public class Answer
{
    public int Id { get; set; }
    public int QuestionId { get; set; }
    public int VoteCount { get; set; }
    public string Content { get; set; }
    public int DisplayOrder { get; set; }
    public int AddTime { get; set; }
    public int UpdateTime { get; set; }
    public byte QStatus { get; set; }
}