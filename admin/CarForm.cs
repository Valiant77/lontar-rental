using System.Text.RegularExpressions;
using MySqlConnector;

namespace Rentalin;
class CarForm : UserControl
{
    readonly Label heading;
    readonly TextBox nameBox;
    readonly TextBox colorBox;
    readonly NumericUpDown seatsBox;
    readonly TextBox plateBox;
    readonly NumericUpDown priceBox;
    readonly CheckBox damagedBox;
    readonly PictureBox preview;
    readonly Button removePhotoButton;

    int? carId;
    string existingImage = "";
    string? newImageFile;
    bool imageRemoved;

    public event Action? Saved;
    public event Action? Cancelled;

    static Label Lbl(string text) => new Label {
        Text = text,
        ForeColor = Theme.TextMuted,
        AutoSize = true,
        Margin = new Padding(0, 0, 0, 4)
    };

    static TextBox Box() => new TextBox {
        Anchor = AnchorStyles.Left | AnchorStyles.Right,
        Font = new Font("Segoe UI", 11f),
        BorderStyle = BorderStyle.FixedSingle,
        Margin = new Padding(0, 0, 16, 14)
    };

    public CarForm()
    {
        Dock = DockStyle.Fill;
        var card = new Panel {
            Dock = DockStyle.Fill,
            BackColor = Theme.Card,
            Padding = new Padding(30)
        };
        card.Paint += (s, e) => {
            using var pen = new Pen(Theme.Border);
            e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
        };

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50)); //heading
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); //fields + photo
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60)); //buttons

        heading = new Label {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI Semibold", 16f),
            TextAlign = ContentAlignment.MiddleLeft
        };

        var body = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        body.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        //left fields
        var left = new TableLayoutPanel {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 9,
            Margin = new Padding(0, 0, 24, 0)
        };
        left.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
        left.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
        for (int i = 0; i < 9; i++) left.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        nameBox = Box();
        nameBox.MaxLength = 256;
        nameBox.PlaceholderText = "Contoh: Blista Compact 2003";
        nameBox.Margin = new Padding(0, 0, 0, 14);
        nameBox.TextChanged += (s, e) => {
            string n = nameBox.Text.Trim();
            heading.Text = "Data Mobil | " + (n.Length > 0 ? n : "Mobil Baru");
        };

        colorBox = Box();
        colorBox.MaxLength = 32;

        seatsBox = new NumericUpDown {
            Minimum = 1, Maximum = 50, Value = 4,
            Anchor = AnchorStyles.Left | AnchorStyles.Right,
            Font = new Font("Segoe UI", 11f),
            Margin = new Padding(0, 0, 0, 14)
        };

        plateBox = Box();
        plateBox.MaxLength = 16;
        plateBox.CharacterCasing = CharacterCasing.Upper;
        plateBox.Margin = new Padding(0, 0, 0, 14);

        priceBox = new NumericUpDown {
            Minimum = 0, Maximum = 100_000_000, Increment = 5000,
            ThousandsSeparator = true,
            Anchor = AnchorStyles.Left | AnchorStyles.Right,
            Font = new Font("Segoe UI", 11f),
            Margin = new Padding(0, 0, 0, 14)
        };

        damagedBox = new CheckBox {
            Text = "Mobil dalam kondisi rusak",
            AutoSize = true,
            Margin = new Padding(0, 4, 0, 0)
        };

        var nameLabel = Lbl("Nama Mobil");
        var plateLabel = Lbl("Nomor Polisi");
        var priceLabel = Lbl("Harga per Hari (Rp)");

        left.Controls.Add(nameLabel, 0, 0);   left.SetColumnSpan(nameLabel, 2);
        left.Controls.Add(nameBox, 0, 1);     left.SetColumnSpan(nameBox, 2);
        left.Controls.Add(Lbl("Warna"), 0, 2);
        left.Controls.Add(Lbl("Jumlah Kursi"), 1, 2);
        left.Controls.Add(colorBox, 0, 3);
        left.Controls.Add(seatsBox, 1, 3);
        left.Controls.Add(plateLabel, 0, 4);  left.SetColumnSpan(plateLabel, 2);
        left.Controls.Add(plateBox, 0, 5);    left.SetColumnSpan(plateBox, 2);
        left.Controls.Add(priceLabel, 0, 6);  left.SetColumnSpan(priceLabel, 2);
        left.Controls.Add(priceBox, 0, 7);    left.SetColumnSpan(priceBox, 2);
        left.Controls.Add(damagedBox, 0, 8);  left.SetColumnSpan(damagedBox, 2);

        //right photo
        var right = new TableLayoutPanel {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Margin = new Padding(24, 0, 0, 0)
        };
        right.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        right.RowStyles.Add(new RowStyle(SizeType.AutoSize)); //label
        right.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); //preview
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 56)); //buttons

        preview = new PictureBox {
            Dock = DockStyle.Fill,
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.FromArgb(238, 240, 243),
            Margin = new Padding(0, 0, 0, 12)
        };
        preview.Paint += (s, e) => {
            if (preview.Image != null) return;
            TextRenderer.DrawText(e.Graphics, "Foto mobil disini", new Font("Segoe UI Semibold", 14f),
                preview.ClientRectangle, Theme.TextMuted,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        };

        var uploadButton = new Button { Text = "Upload", Dock = DockStyle.Fill, Margin = new Padding(0, 0, 8, 0) };
        Theme.StyleSecondary(uploadButton);
        uploadButton.Click += (s, e) => Upload();

        removePhotoButton = new Button { Text = "Hapus", Dock = DockStyle.Fill, Margin = new Padding(8, 0, 0, 0), Enabled = false };
        Theme.StyleSecondary(removePhotoButton);
        removePhotoButton.Click += (s, e) => {
            SetPreview(null);
            newImageFile = null;
            imageRemoved = true;
        };

        var photoButtons = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        photoButtons.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        photoButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        photoButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        photoButtons.Controls.Add(uploadButton, 0, 0);
        photoButtons.Controls.Add(removePhotoButton, 1, 0);

        right.Controls.Add(Lbl("Foto Mobil"), 0, 0);
        right.Controls.Add(preview, 0, 1);
        right.Controls.Add(photoButtons, 0, 2);

        body.Controls.Add(left, 0, 0);
        body.Controls.Add(right, 1, 0);

        //the bottoms (hm)
        var backButton = new Button {
            Text = "<< Kembali",
            Size = new Size(160, 44),
            Margin = new Padding(0, 8, 12, 8)
        };
        Theme.StyleSecondary(backButton);
        backButton.Click += (s, e) => Cancelled?.Invoke();

        var saveButton = new Button {
            Text = "Simpan >>",
            Size = new Size(160, 44),
            Margin = new Padding(0, 8, 0, 8)
        };
        Theme.StylePrimary(saveButton);
        saveButton.Click += (s, e) => Save();

        var buttons = new FlowLayoutPanel {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };
        buttons.Controls.Add(backButton);
        buttons.Controls.Add(saveButton);

        layout.Controls.Add(heading, 0, 0);
        layout.Controls.Add(body, 0, 1);
        layout.Controls.Add(buttons, 0, 2);
        card.Controls.Add(layout);
        Controls.Add(card);

        VisibleChanged += (s, e) => { if (Visible) nameBox.Focus(); };
    }
    public void Open(int? id) //if there is id then edit else add new
    {
        carId = id;
        existingImage = "";
        newImageFile = null;
        imageRemoved = false;

        nameBox.Clear();
        colorBox.Clear();
        plateBox.Clear();
        seatsBox.Value = 4;
        priceBox.Value = 0;
        damagedBox.Checked = false;
        SetPreview(null);

        if (id != null) LoadExisting(id.Value);
    }

    void LoadExisting(int id)
    {
        try
        {
            using var conn = Db.Open();
            using var cmd = new MySqlCommand(
                @"SELECT name, color, seats, plate_num, price, damaged, image_path
                  FROM cars WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("@id", id);
            using var r = cmd.ExecuteReader();
            if (!r.Read()) return;

            nameBox.Text = r.GetString("name");
            colorBox.Text = r.GetString("color");
            seatsBox.Value = Math.Clamp(r.GetInt32("seats"), 1, 50);
            plateBox.Text = r.GetString("plate_num");
            priceBox.Value = Math.Clamp(r.GetInt32("price"), 0, 100_000_000);
            damagedBox.Checked = r.GetBoolean("damaged");
            existingImage = r.GetString("image_path");
        }
        catch (MySqlException ex)
        {
            MessageBox.Show("Gagal memuat data mobil:\n" + ex.Message);
            return;
        }

        //show photo if file there
        string file = Path.Combine(AppContext.BaseDirectory, existingImage);
        if (existingImage != "" && File.Exists(file))
        {
            try { SetPreview(LoadBitmap(file)); }
            catch (Exception ex) when (ex is ArgumentException or OutOfMemoryException or IOException) { }
        }
    }

    void SetPreview(Image? image)
    {
        preview.Image?.Dispose();
        preview.Image = image;
        removePhotoButton.Enabled = image != null;
        preview.Invalidate();
    }

    //copied into memory so the file on disk isn't locked
    static Bitmap LoadBitmap(string file)
    {
        using var fs = File.OpenRead(file);
        using var img = Image.FromStream(fs);
        return new Bitmap(img);
    }

    void Upload()
    {
        using var dialog = new OpenFileDialog {
            Title = "Pilih foto mobil",
            Filter = "Gambar|*.jpg;*.jpeg;*.png;*.bmp"
        };
        if (dialog.ShowDialog() != DialogResult.OK) return;

        try
        {
            SetPreview(LoadBitmap(dialog.FileName));
            newImageFile = dialog.FileName;
            imageRemoved = false;
        }
        catch (Exception ex) when (ex is ArgumentException or OutOfMemoryException or IOException)
        {
            MessageBox.Show("File bukan gambar yang valid.", "Gagal",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    static void Warn(string message, Control focus)
    {
        MessageBox.Show(message, "Data belum lengkap", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        focus.Focus();
    }

    static void DeleteQuietly(string? file)
    {
        if (file == null) return;
        try { File.Delete(file); } catch { }
    }

    //copied to images/cars
    static string CopyPhoto(string source, out string target)
    {
        string ext = Path.GetExtension(source).ToLowerInvariant();
        string relative = $"images/cars/{Guid.NewGuid():N}{ext}";
        target = Path.Combine(AppContext.BaseDirectory, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        File.Copy(source, target);
        return relative;
    }

    void Save()
    {
        string name = nameBox.Text.Trim();
        string color = colorBox.Text.Trim();
        string plate = Regex.Replace(plateBox.Text.Trim(), @"\s+", " ").ToUpperInvariant();
        int seats = (int)seatsBox.Value;
        int price = (int)priceBox.Value;

        if (name.Length < 2)  { Warn("Nama mobil wajib diisi.", nameBox); return; }
        if (color.Length < 2) { Warn("Warna wajib diisi.", colorBox); return; }
        if (plate.Length < 3) { Warn("Nomor polisi wajib diisi.", plateBox); return; }
        if (price <= 0)       { Warn("Harga per hari harus lebih dari 0.", priceBox); return; }

        string? copiedFile = null;
        try
        {
            string image = existingImage;
            if (newImageFile != null) image = CopyPhoto(newImageFile, out copiedFile);
            else if (imageRemoved) image = "";

            using var conn = Db.Open();
            using var tx = conn.BeginTransaction();

            int id;
            if (carId == null)
            {
                using var cmd = new MySqlCommand(
                    @"INSERT INTO cars (image_path, name, color, seats, plate_num, price, damaged)
                      VALUES (@img, @n, @c, @s, @p, @price, @d)", conn, tx);
                AddParams(cmd, image, name, color, seats, plate, price);
                cmd.ExecuteNonQuery();
                id = (int)cmd.LastInsertedId;
            }
            else
            {
                using var cmd = new MySqlCommand(
                    @"UPDATE cars SET image_path = @img, name = @n, color = @c, seats = @s,
                                      plate_num = @p, price = @price, damaged = @d
                      WHERE id = @id", conn, tx);
                AddParams(cmd, image, name, color, seats, plate, price);
                cmd.Parameters.AddWithValue("@id", carId.Value);
                cmd.ExecuteNonQuery();
                id = carId.Value;
            }

            Db.Log(conn, tx, carId == null ? "car_create" : "car_update", "car", id, $"{name} ({plate})");
            tx.Commit();
        }
        catch (MySqlException ex) when (ex.ErrorCode == MySqlErrorCode.DuplicateKeyEntry)
        {
            DeleteQuietly(copiedFile);
            Warn("Nomor polisi sudah terdaftar.", plateBox);
            return;
        }
        catch (Exception ex) when (ex is MySqlException or IOException)
        {
            DeleteQuietly(copiedFile);
            MessageBox.Show("Gagal menyimpan:\n" + ex.Message);
            return;
        }

        Saved?.Invoke();
    }

    void AddParams(MySqlCommand cmd, string image, string name, string color,
                   int seats, string plate, int price)
    {
        cmd.Parameters.AddWithValue("@img", image);
        cmd.Parameters.AddWithValue("@n", name);
        cmd.Parameters.AddWithValue("@c", color);
        cmd.Parameters.AddWithValue("@s", seats);
        cmd.Parameters.AddWithValue("@p", plate);
        cmd.Parameters.AddWithValue("@price", price);
        cmd.Parameters.AddWithValue("@d", damagedBox.Checked);
    }
}