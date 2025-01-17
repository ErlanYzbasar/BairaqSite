namespace MODEL;
public partial class Advertise
{
    public int Id { get; set; }
    public string ThumbnailUrl { get; set; }
    public string Title { get; set; }
    public string TargetType { get; set; }
    public string AdUrl { get; set; }
    public int DisplayOrder { get; set; }
    public int ViewCount{ get; set; }
    public string Language { get; set; }
    public string Location { get; set; }
    public string AdType { get; set; }
    public int AddTime { get; set; }
    public int UpdateTime { get; set; }
    public byte QStatus { get; set; }
}