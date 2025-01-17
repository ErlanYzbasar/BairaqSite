namespace MODEL;

public class Currency
{
    public int DisplayOrder { get; set; }
    public uint Fixed { get; set; }
    public uint HandlingFee { get; set; }
    public int Id { get; set; }
    public uint IntRatio { get; set; }
    public byte IsAutomatic { get; set; }
    public byte QStatus { get; set; }
    public uint Rate { get; set; }
    public string Symbol { get; set; }
    public string Title { get; set; }
    public string Type { get; set; }
    public int UpdateTime { get; set; }
}