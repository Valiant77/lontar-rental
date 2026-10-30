using MySqlConnector;
using System.Data;
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
    public static void Log(MySqlConnection conn, MySqlTransaction? tx, string action, string? targetType = null, int? targetId = null, string? details = null)
    {
        using var cmd = new MySqlCommand(
            @"INSERT INTO activity_log (user_id, action, target_type, target_id, details)
            VALUES (@u, @a, @tt, @ti, @d)", conn, tx);
        cmd.Parameters.AddWithValue("@u", Session.UserId == 0 ? DBNull.Value : (object)Session.UserId);
        cmd.Parameters.AddWithValue("@a", action);
        cmd.Parameters.AddWithValue("@tt", (object?)targetType ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ti", (object?)targetId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@d", (object?)details ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }
    public static void Log(string action, string? targetType = null, int? targetId = null, string? details = null)
    {
        using var conn = Open();
        Log(conn, null, action, targetType, targetId, details);
    }
    public static DataTable Query(string sql)
    {
        using var conn = Open();
        using var cmd = new MySqlCommand(sql, conn);
        using var reader = cmd.ExecuteReader();
        var table = new DataTable();
        table.Load(reader);
        return table;
    }
    
    public static List<Car> LoadCars()
    {
        var cars = new List<Car>();

        using var conn = Open();
        using var cmd = new MySqlCommand(@"SELECT id, image_path, name, color, seats, plate_num, price, damaged, status FROM cars ORDER BY damaged, name", conn);
        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            cars.Add(new Car(
                reader.GetInt32("id"),
                reader.GetString("name"),
                reader.GetString("color"),
                reader.GetInt32("seats"),
                reader.GetString("plate_num"),
                reader.GetInt32("price"),
                reader.GetBoolean("damaged"),
                reader.GetString("status"),
                reader.GetString("image_path")
            ));
        }
        return cars;
    }
}