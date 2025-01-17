namespace MODEL.FormatModels;

public class ApiUnifiedModel
{
    public uint Id { get; set; }
    public string Keyword { get; set; }
    public string DateTimeStart { get; set; }
    public string DateTimeEnd { get; set; }
    public int Start { get; set; }
    public int Length { get; set; }
    public List<DataTableOrderModel> OrderList { get; set; }
}