using MySqlConnector;

namespace Rentalin;

static class Db
{
    const string ConnStr = "Server=localhost;Port=3306;Database=rentalin;User=root;Password=Valiant77;";
    public static MySqlConnection Open()
    {
        var conn = new MySqlConnection(ConnStr);
        conn.Open();
        return conn;
    }
}