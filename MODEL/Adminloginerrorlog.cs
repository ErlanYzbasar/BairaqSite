namespace MODEL;

public class Adminloginerrorlog
{
    public int AdminId { get; set; }
    public int ErrorCount { get; set; }
    public uint Id { get; set; }
    public string LastErrorIp { get; set; }
    public int LastErrorTime { get; set; }
}