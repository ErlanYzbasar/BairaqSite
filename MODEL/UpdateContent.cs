using System.ComponentModel.DataAnnotations.Schema;
using MODEL.Enums;

namespace MODEL;

public class UpdateContent
{
    public int Id { get; set; }
    public int LogId { get; set; }
    public string ColumnName { get; set; }
    public Operation Operation { get; set; }
    public string Content { get; set; }
    [NotMapped] public bool IsInsert => Operation.INSERT.Equals(Operation);
}