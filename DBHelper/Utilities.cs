using System.Data;
using COMMON;
using MySql.Data.MySqlClient;

namespace DBHelper;

public class Utilities
{
    public static IDbConnection GetOpenConnection()
    {
        var connectionString = QarSingleton.GetInstance().GetConnectionString();
        IDbConnection connection = new MySqlConnection(connectionString);
        connection.Open();
        return connection;
    }

    public static IDbConnection GetOldDbConnection()
    {
        var connectionString =
            "Server=localhost;port=3306;database=qamshy_db;uid=root;pwd=Talant0227;charset=utf8mb4;";
        IDbConnection connection = new MySqlConnection(connectionString);
        connection.Open();
        return connection;
    }

    public static IDbConnection GetOldServerDbConnection()
    {
        var connectionString =
            "Server=45.82.31.169;port=3306;database=almatyakshamy_db;uid=test_dba;pwd=u2qJMVc4>Q{AY#m$^WH[pTf!Zksa5;charset=utf8mb4;";
        IDbConnection connection = new MySqlConnection(connectionString);
        connection.Open();
        return connection;
    }
}