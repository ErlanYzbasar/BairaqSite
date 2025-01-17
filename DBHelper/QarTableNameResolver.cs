using Dapper;

namespace DBHelper;

public class QarTableNameResolver : SimpleCRUD.ITableNameResolver
{
    public string ResolveTableName(Type type)
    {
        return type.Name.ToLower();
    }
}