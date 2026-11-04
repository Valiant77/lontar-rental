using MySqlConnector;

namespace Rentalin;

class RentalException : Exception
{
    public RentalException(string message) : base(message) { }
}

static class RentalService
{
    //from how convoluted it is, I decide to put the insert to the db separate instead doing it in the rentascashier form
    public static string Create(Car car, Renter renter, RenterDocs docs, LoanTerms terms)
    {
        if (Session.UserId == 0)
            throw new RentalException("Sesi login tidak valid. Silakan login ulang.");

        var copied = new List<string>();
        try
        {
            using var conn = Db.Open();
            using var tx = conn.BeginTransaction(); 

            string invoice = NewInvoiceCode(conn, tx);

            //1. documents into the docs folder
            string ktpPath = CopyDocument(docs.KtpPath, invoice, "ktp", copied);
            string simPath = CopyDocument(docs.SimPath, invoice, "sim", copied);

            //2. renter: new row, or update if this NIK is already registered
            using (var cmd = new MySqlCommand(
                @"INSERT INTO renters (nik, name, gender, birthdate, birthplace, phone, email, address, id_card, license)
                  VALUES (@nik, @name, @gender, @birthdate, @birthplace, @phone, @email, @address, @idcard, @license)
                  ON DUPLICATE KEY UPDATE
                    name = VALUES(name), gender = VALUES(gender), birthdate = VALUES(birthdate),
                    birthplace = VALUES(birthplace), phone = VALUES(phone), email = VALUES(email),
                    address = VALUES(address), id_card = VALUES(id_card), license = VALUES(license)",
                conn, tx))
            {
                cmd.Parameters.AddWithValue("@nik", renter.NIK);
                cmd.Parameters.AddWithValue("@name", renter.Name);
                cmd.Parameters.AddWithValue("@gender", renter.Gender == "Perempuan" ? "female" : "male");
                cmd.Parameters.AddWithValue("@birthdate", renter.BirthDate);
                cmd.Parameters.AddWithValue("@birthplace", renter.BirthPlace);
                cmd.Parameters.AddWithValue("@phone", renter.Phone);
                cmd.Parameters.AddWithValue("@email", renter.Email);
                cmd.Parameters.AddWithValue("@address", renter.Address);
                cmd.Parameters.AddWithValue("@idcard", ktpPath);
                cmd.Parameters.AddWithValue("@license", simPath);
                cmd.ExecuteNonQuery();
            }

            int renterId;
            using (var cmd = new MySqlCommand("SELECT id FROM renters WHERE nik = @nik", conn, tx))
            {
                cmd.Parameters.AddWithValue("@nik", renter.NIK);
                renterId = Convert.ToInt32(cmd.ExecuteScalar());
            }

            //3. car now borrowed, but only if it is still available
            using (var cmd = new MySqlCommand(
                "UPDATE cars SET status = 'dipinjam' WHERE id = @id AND status = 'tersedia'", conn, tx))
            {
                cmd.Parameters.AddWithValue("@id", car.Id);
                if (cmd.ExecuteNonQuery() != 1)
                    throw new RentalException("Mobil ini sudah tidak tersedia (mungkin baru saja dipinjam kasir lain).");
            }

            //4. create new rental row
            long rentalId;
            using (var cmd = new MySqlCommand(
                @"INSERT INTO rentals (invoice_code, renter_id, car_id, cashier_id, start_date, due_date,
                                       price_per_day, total, status, fine)
                  VALUES (@inv, @renter, @car, @cashier, @start, @due, @price, @total, 'ongoing', 0)",
                conn, tx))
            {
                cmd.Parameters.AddWithValue("@inv", invoice);
                cmd.Parameters.AddWithValue("@renter", renterId);
                cmd.Parameters.AddWithValue("@car", car.Id);
                cmd.Parameters.AddWithValue("@cashier", Session.UserId);
                cmd.Parameters.AddWithValue("@start", terms.LoanDate);
                cmd.Parameters.AddWithValue("@due", terms.ReturnDate);
                cmd.Parameters.AddWithValue("@price", car.Price);
                cmd.Parameters.AddWithValue("@total", (int)terms.Total);
                cmd.ExecuteNonQuery();
                rentalId = cmd.LastInsertedId;
            }

            //5. logged in
            Db.Log(conn, tx, "rental_create", "rental", (int)rentalId,
                   $"{invoice} | {car.Name} ({car.Plate}) | {renter.Name}");

            tx.Commit();
            return invoice;
        }
        catch
        {
            foreach (var file in copied)
            {
                try { File.Delete(file); } catch {  }
            }
            throw;
        }
    }

    static string NewInvoiceCode(MySqlConnection conn, MySqlTransaction tx)
    {
        while (true)
        {
            string code = $"LNTRRNTL{DateTime.Now:yyMMdd}{Random.Shared.Next(0, 10000):D4}";
            using var cmd = new MySqlCommand(
                "SELECT COUNT(*) FROM rentals WHERE invoice_code = @c", conn, tx);
            cmd.Parameters.AddWithValue("@c", code);
            if (Convert.ToInt32(cmd.ExecuteScalar()) == 0) return code;
        }
    }

    static string CopyDocument(string source, string invoice, string kind, List<string> copied)
    {
        string ext = Path.GetExtension(source).ToLowerInvariant();
        string relative = $"docs/{invoice}_{kind}{ext}";
        string target = Path.Combine(AppContext.BaseDirectory, relative);

        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        File.Copy(source, target, overwrite: false);
        copied.Add(target);
        return relative;
    }

    public static Loan? Find(string invoice)
    {
        using var conn = Db.Open();
        using var cmd = new MySqlCommand(
            @"SELECT r.id, r.car_id, r.invoice_code, r.start_date, r.due_date,
                    r.price_per_day, r.total, r.returned_at,
                    c.name AS car_name, c.color, c.plate_num,
                    rt.name AS renter_name, rt.birthplace, rt.birthdate, rt.address,
                    u.name AS cashier_name
            FROM rentals r
            JOIN cars c ON c.id  = r.car_id
            JOIN renters rt ON rt.id = r.renter_id
            JOIN users u ON u.id  = r.cashier_id
            WHERE r.invoice_code = @c
            LIMIT 1", conn);
        cmd.Parameters.AddWithValue("@c", invoice);

        using var r = cmd.ExecuteReader();
        if (!r.Read()) return null;

        int returnedAt = r.GetOrdinal("returned_at");
        return new Loan(
            r.GetInt32("id"),
            r.GetInt32("car_id"),
            r.GetString("invoice_code"),
            $"{r.GetString("car_name")}, {r.GetString("color")}",
            r.GetString("plate_num"),
            r.GetString("renter_name"),
            $"{r.GetString("birthplace")}, {r.GetDateTime("birthdate"):dd/MM/yyyy}",
            r.GetString("address"),
            r.GetString("cashier_name"),
            r.GetDateTime("start_date"),
            r.GetDateTime("due_date"),
            r.GetInt32("price_per_day"),
            r.GetInt32("total"),
            r.IsDBNull(returnedAt) ? null : r.GetDateTime(returnedAt));
    }

    public static int LateDays(DateTime due, DateTime today) =>
        Math.Max(0, (today.Date - due.Date).Days);

    //1.5x the daily price, per late day
    public static int Fine(int pricePerDay, int lateDays) =>
        (int)Math.Round(lateDays * pricePerDay * 1.5m);

    //returns the fine that was charged
    public static int Return(Loan loan)
    {
        if (Session.UserId == 0)
            throw new RentalException("Sesi login tidak valid. Silakan login ulang.");

        var today = DateTime.Today;
        int fine = Fine(loan.PricePerDay, LateDays(loan.DueDate, today));

        using var conn = Db.Open();
        using var tx = conn.BeginTransaction();

        using (var cmd = new MySqlCommand(
            @"UPDATE rentals SET returned_at = @today, fine = @fine, status = 'finished'
            WHERE id = @id AND status = 'ongoing'", conn, tx))
        {
            cmd.Parameters.AddWithValue("@today", today);
            cmd.Parameters.AddWithValue("@fine", fine);
            cmd.Parameters.AddWithValue("@id", loan.Id);
            if (cmd.ExecuteNonQuery() != 1)
                throw new RentalException("Peminjaman ini sudah dikembalikan.");
        }

        using (var cmd = new MySqlCommand(
            "UPDATE cars SET status = 'tersedia' WHERE id = @car AND status = 'dipinjam'", conn, tx))
        {
            cmd.Parameters.AddWithValue("@car", loan.CarId);
            cmd.ExecuteNonQuery();
        }

        Db.Log(conn, tx, "rental_return", "rental", loan.Id,
            $"{loan.Invoice} | {loan.Vehicle} ({loan.Plate}) | denda Rp{fine:N0}");

        tx.Commit();
        return fine;
    }
}