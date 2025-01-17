namespace MODEL;

public class UpdateLog
{
    public int Id { get; set; }
    public string TableName { get; set; }
    public int ItemId { get; set; }
    public int AdminId { get; set; }
    public int AddTime { get; set; }
    public byte QStatus { get; set; }
}