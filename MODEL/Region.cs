namespace MODEL;

public class Region
{
    public string Alias { get; set; }
    public int ChildrenCount { get; set; }
    public string CnKey { get; set; }
    public string Code { get; set; }
    public int DisplayOrder { get; set; }
    public uint Id { get; set; }
    public byte IsBigCity { get; set; }
    public uint KrishaId { get; set; }
    public float Latitude { get; set; }
    public int Level { get; set; }
    public float Longitude { get; set; }
    public string Name { get; set; }
    public uint ParentId { get; set; }
    public string PostCode { get; set; }
    public byte QStatus { get; set; }
    public string Type { get; set; }
    public int WarehouseCount { get; set; }
}