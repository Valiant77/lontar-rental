using MySqlConnector;

namespace Rentalin;

record DayIncome(DateTime Day, int Rental, int Fine);
record TopCar(string Name, string Plate, int Count);
record MapPin(string Plate, bool Overdue);
record Overview(int TodayRentals, int Active, int Overdue,
List<DayIncome> Week, List<TopCar> TopCars,
List<(string Label, double Value)> PieNew,
List<(string Label, double Value)> PieCars,
List<MapPin> Pins);

static class Stats
{
    static MySqlCommand Cmd(MySqlConnection conn, string sql, DateTime today)
    {
        var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@today", today);
        cmd.Parameters.AddWithValue("@since", today.AddDays(-6));   //last 7 days incl today
        return cmd;
    }

    static int Int(MySqlConnection conn, string sql, DateTime today)
    {
        using var cmd = Cmd(conn, sql, today);
        object? v = cmd.ExecuteScalar();
        return v == null || v is DBNull ? 0 : Convert.ToInt32(v);
    }

    public static Overview Load()
    {
        var today = DateTime.Today;
        var since = today.AddDays(-6);
        using var conn = Db.Open();

        //cards
        int todayRentals = Int(conn, "SELECT COUNT(*) FROM rentals WHERE start_date = @today", today);
        int active = Int(conn, "SELECT COUNT(*) FROM rentals WHERE status = 'ongoing'", today);
        int overdue = Int(conn,
            "SELECT COUNT(*) FROM rentals WHERE status = 'ongoing' AND due_date < @today", today);

        //income
        var rental = new int[7];
        var fine = new int[7];
        using (var cmd = Cmd(conn,
            @"SELECT d, SUM(rental) AS rental, SUM(fine) AS fine FROM (
                SELECT start_date AS d, total AS rental, 0 AS fine
                  FROM rentals WHERE start_date >= @since
                UNION ALL
                SELECT returned_at AS d, 0, fine
                  FROM rentals WHERE returned_at >= @since
              ) x GROUP BY d", today))
        using (var r = cmd.ExecuteReader())
        {
            while (r.Read())
            {
                int i = (r.GetDateTime("d").Date - since).Days;
                if (i < 0 || i > 6) continue;
                rental[i] = Convert.ToInt32(r["rental"]);
                fine[i] = Convert.ToInt32(r["fine"]);
            }
        }
        var week = Enumerable.Range(0, 7)
            .Select(i => new DayIncome(since.AddDays(i), rental[i], fine[i]))
            .ToList();

        //top five cars
        var top = new List<TopCar>();
        using (var cmd = Cmd(conn,
            @"SELECT c.name, c.plate_num, COUNT(*) AS cnt
              FROM rentals r JOIN cars c ON c.id = r.car_id
              WHERE r.start_date >= @since
              GROUP BY c.id, c.name, c.plate_num
              ORDER BY cnt DESC, SUM(r.total) DESC
              LIMIT 5", today))
        using (var r = cmd.ExecuteReader())
        {
            while (r.Read())
                top.Add(new TopCar(r.GetString("name"), r.GetString("plate_num"), r.GetInt32("cnt")));
        }
        //pies
        //pie 1: new vs returning renters (by their first-ever rental)
        int newCount = 0, returningCount = 0;
        using (var cmd = Cmd(conn,
            @"SELECT COALESCE(SUM(first_start >= @since), 0) AS new_count,
                     COALESCE(SUM(first_start <  @since), 0) AS returning_count
              FROM (SELECT renter_id, MIN(start_date) AS first_start, MAX(start_date) AS last_start
                    FROM rentals GROUP BY renter_id) t
              WHERE last_start >= @since", today))
        using (var r = cmd.ExecuteReader())
        {
            if (r.Read())
            {
                newCount = Convert.ToInt32(r["new_count"]);
                returningCount = Convert.ToInt32(r["returning_count"]);
            }
        }
        var pieNew = new List<(string Label, double Value)> {
            ("Baru", newCount),
            ("Lama", returningCount)
        };
        //pie 2: rental income per car, top 5 + "Lainnya"
        var perCar = new List<(string Label, double Value)>();
        using (var cmd = Cmd(conn,
            @"SELECT CONCAT(c.name, ' (', c.plate_num, ')') AS label, SUM(r.total) AS income
              FROM rentals r JOIN cars c ON c.id = r.car_id
              WHERE r.start_date >= @since
              GROUP BY c.id, c.name, c.plate_num
              ORDER BY income DESC", today))
        using (var r = cmd.ExecuteReader())
        {
            while (r.Read())
                perCar.Add((r.GetString("label"), Convert.ToDouble(r["income"])));
        }
        var pieCars = perCar.Take(5).ToList();
        double rest = perCar.Skip(5).Sum(x => x.Value);
        if (rest > 0) pieCars.Add(("Lainnya", rest));

        //map pins: every car that is out right now (fake tho)
        var pins = new List<MapPin>();
        using (var cmd = Cmd(conn,
            @"SELECT c.plate_num, (r.due_date < @today) AS late
              FROM rentals r JOIN cars c ON c.id = r.car_id
              WHERE r.status = 'ongoing'", today))
        using (var r = cmd.ExecuteReader())
        {
            while (r.Read())
                pins.Add(new MapPin(r.GetString("plate_num"), Convert.ToInt32(r["late"]) == 1));
        }

        return new Overview(todayRentals, active, overdue, week, top, pieNew, pieCars, pins);
    }
}