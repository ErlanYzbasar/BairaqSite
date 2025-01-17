namespace MODEL;

public class Rating
{
    public int ArticleId { get; set; }
    public uint Dissatisfied { get; set; }
    public int Funny { get; set; }
    public int Id { get; set; }
    public uint Outrageous { get; set; }
    public byte QStatus { get; set; }
    public int Satisfied { get; set; }
}