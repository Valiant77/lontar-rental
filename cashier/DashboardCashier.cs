using MySqlConnector;
namespace Rentalin;
record Car(int Id, string Name, string Color, int Seats, string Plate, int Price, bool Damaged, string Status, string? ImagePath);
static class Theme
{
    //dont mind us we are just a bunch of 'CSS imported to C#'
    public static readonly Color Background = Color.FromArgb(245, 246, 248);
    public static readonly Color Card = Color.White;
    public static readonly Color Accent = Color.FromArgb(37, 99, 235);
    public static readonly Color AccentHover = Color.FromArgb(29, 78, 216);
    public static readonly Color AccentSoft = Color.FromArgb(235, 242, 254);
    public static readonly Color Border = Color.FromArgb(225, 228, 232);
    public static readonly Color TextMuted = Color.FromArgb(100, 107, 118);
    public static readonly Color TextDark = Color.FromArgb(30, 41, 59);
    public static readonly Color Danger = Color.FromArgb(220, 38, 38);
    public static readonly Color DangerHover = Color.FromArgb(185, 28, 28);
    public static void StylePrimary(Button b)
    {
        b.FlatStyle = FlatStyle.Flat;
        b.FlatAppearance.BorderSize = 0;
        b.BackColor = Accent;
        b.ForeColor = Color.White;
        b.Font = new Font("Segoe UI Semibold", 10f);
        b.Cursor = Cursors.Hand;
        b.MouseEnter += (s, e) => b.BackColor = AccentHover;
        b.MouseLeave += (s, e) => b.BackColor = Accent;
    }
    public static void StyleSecondary(Button b)
    {
        b.FlatStyle = FlatStyle.Flat;
        b.FlatAppearance.BorderColor = Border;
        b.BackColor = Color.White;
        b.Font = new Font("Segoe UI", 10f);
        b.Cursor = Cursors.Hand;
    }
    public static void StyleDanger(Button b)
    {
        b.FlatStyle = FlatStyle.Flat;
        b.FlatAppearance.BorderSize = 0;
        b.BackColor = Danger;
        b.ForeColor = Color.White;
        b.Font = new Font("Segoe UI Semibold", 10f);
        b.Cursor = Cursors.Hand;
        b.MouseEnter += (s, e) => b.BackColor = DangerHover;
        b.MouseLeave += (s, e) => b.BackColor = Danger;
    }
    public static void StyleDisabled(Button b)
    {
        b.FlatStyle = FlatStyle.Flat;
        b.FlatAppearance.BorderSize = 0;
        b.BackColor = Color.FromArgb(238, 240, 243);
        b.Font = new Font("Segoe UI Semibold", 10f);
        b.Enabled = false;
    }
}

public partial class DashboardCashier : DashboardBase
{
    List<Car> allCars = new();
    readonly string name;
    readonly Label greetingLabel;
    readonly FlowLayoutPanel carGrid;
    readonly Control carsPage;
    readonly ReturnAsCashier returnAsCashier;
    readonly RentAsCashier rentAsCashier;
    readonly Button navCars;
    readonly Button navReturn;

    public DashboardCashier(string name) {
        this.name = name;
        AutoScaleMode = AutoScaleMode.Font;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = true;
        WindowState = FormWindowState.Maximized;
        Text = "Dashboard Kasir | " + name;
        BackColor = Theme.Background;
        Font = new Font("Segoe UI", 10f);
        ForeColor = Theme.TextDark;

        var root = new TableLayoutPanel {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 110)); //navbar
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); //content

        var navbar = new TableLayoutPanel {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 1,
            BackColor = Theme.Card,
            Padding = new Padding(20, 0, 20, 0)
        };
        navbar.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        navbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120)); //logo
        navbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 260)); //greeting
        navbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); //nav items
        navbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120)); //logout
        navbar.Paint += (s, e) => {
            using var pen = new Pen(Theme.Border);
            e.Graphics.DrawLine(pen, 0, navbar.Height - 1, navbar.Width, navbar.Height - 1);
        };

        Image logoImage = Image.FromFile(Path.Combine(AppContext.BaseDirectory, "images", "clogo.png"));
        var logo = new PictureBox {
            Image = logoImage,
            SizeMode = PictureBoxSizeMode.Zoom,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 8, 16, 10)
        };

        greetingLabel = new Label {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 11f)
        };

        navCars = MakeNavButton("Peminjaman");
        navReturn = MakeNavButton("Pengembalian");
        var navItems = new FlowLayoutPanel {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = false,
            Anchor = AnchorStyles.Left
        };
        navItems.Controls.Add(navCars);
        navItems.Controls.Add(navReturn);

        var logoutButton = new Button {
            Text = "Logout",
            Size = new Size(100, 44),
            Anchor = AnchorStyles.Right
        };
        Theme.StyleDanger(logoutButton);
        logoutButton.Click += (s, e) => {
            LoggedOut = true;
            Close();
        };

        navbar.Controls.Add(logo, 0, 0);
        navbar.Controls.Add(greetingLabel, 1, 0);
        navbar.Controls.Add(navItems, 2, 0);
        navbar.Controls.Add(logoutButton, 3, 0);

        //ALL PEMINJAMAN STUFF
        var carsLayout = new TableLayoutPanel {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2
        };
        carsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        carsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
        carsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var searchBox = new TextBox {
            PlaceholderText = "Cari mobil...",
            Width = 320,
            Font = new Font("Segoe UI", 11f),
            BorderStyle = BorderStyle.FixedSingle,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(8, 3, 3, 3)
        };
        //TODO: add the search function

        carGrid = new FlowLayoutPanel {
            Dock = DockStyle.Fill,
            AutoScroll = true
        };
        carsLayout.Controls.Add(searchBox, 0, 0);
        carsLayout.Controls.Add(carGrid, 0, 1);
        carsPage = carsLayout;
        
        //ALL PEMINJAMAN STUFF
        rentAsCashier = new RentAsCashier(name) { Visible = false };
        rentAsCashier.Cancelled += () => ShowPage(carsPage);
        rentAsCashier.Finished += () => { LoadCars(); ShowPage(carsPage); };
        //ALL PENGEMBALIAN STUFF
        returnAsCashier = new ReturnAsCashier { Visible = false };
        returnAsCashier.BackRequested += () => ShowPage(carsPage);
        returnAsCashier.Completed += () => { LoadCars(); ShowPage(carsPage); };

        var content = new Panel {
            Dock = DockStyle.Fill,
            Padding = new Padding(20, 15, 20, 20)
        };
        content.Controls.Add(carsPage);
        content.Controls.Add(rentAsCashier);
        content.Controls.Add(returnAsCashier);

        navCars.Click += (s, e) => ShowPage(carsPage);
        navReturn.Click += (s, e) => ShowPage(returnAsCashier);
        ShowPage(carsPage);

        root.Controls.Add(navbar, 0, 0);
        root.Controls.Add(content, 0, 1);
        Controls.Add(root);

        //greeting and clock in which the greeting is updated per 10 secs
        UpdateGreeting();
        var timer = new System.Windows.Forms.Timer { Interval = 10_000 };
        timer.Tick += (s, e) => UpdateGreeting();
        timer.Start();
        FormClosed += (s, e) => timer.Dispose();
        Shown += (s, e) => LoadCars();
    }
    
    static Button MakeNavButton(string text) {
        var b = new Button {
            Text = text,
            Size = new Size(150, 44),
            Margin = new Padding(0, 0, 8, 0),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI Semibold", 10f),
            Cursor = Cursors.Hand
        };
        b.FlatAppearance.BorderSize = 0;
        return b;
    }
    void ShowPage(Control page) {
        carsPage.Visible = page == carsPage;
        returnAsCashier.Visible = page == returnAsCashier;
        rentAsCashier.Visible = page == rentAsCashier;
        SetActive(navCars, page == carsPage || page == rentAsCashier);
        SetActive(navReturn, page == returnAsCashier);
    }

    static void SetActive(Button b, bool active) {
        b.BackColor = active ? Theme.AccentSoft : Theme.Card;
        b.ForeColor = active ? Theme.Accent : Theme.TextMuted;
    }
    void UpdateGreeting() { //update greeting...update greeting?! prepsoterous
        var now = DateTime.Now;
        string sapaan = now.Hour switch {
            < 12 => "Pagi",
            < 15 => "Siang",
            < 18 => "Sore",
            _ => "Malam"
        };
        greetingLabel.Text = $"Selamat {sapaan}, {name}\n{now:dd/MM/yyyy, HH:mm}";
    }
    void LoadCars()
    {
        try
        {
            allCars = Db.LoadCars();
            RenderCars(allCars);
        }
        catch (MySqlException ex)
        {
            MessageBox.Show("Gagal memuat data mobil:\n" + ex.Message);
        }
    }
    void RenderCars(IEnumerable<Car> cars) {
        carGrid.SuspendLayout();

        foreach (Control c in carGrid.Controls) c.Dispose();
        carGrid.Controls.Clear();

        int margin = 8;
        int usable = carGrid.ClientSize.Width - SystemInformation.VerticalScrollBarWidth;
        int cardW = usable / 5 - margin * 2 - 1;
        int pad = 10;
        int pictureH = (cardW - pad * 2) * 3 / 5;

        foreach (var car in cars)
        {
            var card = new Panel {
                Size = new Size(cardW, pictureH + 150),
                Margin = new Padding(margin),
                Padding = new Padding(pad),
                BackColor = Theme.Card
            };
            card.Paint += (s, e) => {
                using var pen = new Pen(Theme.Border);
                e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
            };

            var picture = new PictureBox {
                Dock = DockStyle.Top,
                Height = pictureH,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.FromArgb(238, 240, 243)
            };
            string imgFile = Path.Combine(AppContext.BaseDirectory, car.ImagePath ?? "");
            if (!string.IsNullOrWhiteSpace(car.ImagePath) && File.Exists(imgFile))
                picture.Image = Image.FromFile(imgFile);

            var nameRow = new Panel { Dock = DockStyle.Top, Height = 34 };
            var name = new Label {
                Dock = DockStyle.Fill,
                Text = car.Name,
                Font = new Font("Segoe UI Semibold", 11f),
                AutoEllipsis = true,
                TextAlign = ContentAlignment.BottomLeft
            };
            nameRow.Controls.Add(name);

            if (car.Damaged)
            {
                var tagFont = new Font("Segoe UI Semibold", 11f);   // same as the name
                var tag = new Label {
                    Dock = DockStyle.Right,
                    AutoSize = false,
                    Width = TextRenderer.MeasureText("Damaged", tagFont).Width + 8,
                    Text = "Damaged",
                    ForeColor = Theme.Danger,
                    Font = tagFont,
                    TextAlign = ContentAlignment.BottomRight
                };
                nameRow.Controls.Add(tag);
            }

            var details = new Label {
                Dock = DockStyle.Fill,
                Text = $"{car.Color} | {car.Seats} Kursi\n{car.Plate}",
                ForeColor = Theme.TextMuted,
                Padding = new Padding(0, 4, 0, 0)
            };

            var borrow = new Button {
                Dock = DockStyle.Bottom,
                Height = 38
            };

            if (car.Status == "tersedia") //look yeah i know it is convoluted but shut up
            {
                borrow.Text = "Pinjam";
                Theme.StylePrimary(borrow);
                borrow.Click += (s, e) => {
                    if (car.Damaged)
                    {
                        var answer = MessageBox.Show(
                            $"{car.Name} ({car.Plate}) berstatus rusak.\n\n" + "Beritahu penyewa kondisi kendaraan sebelum melanjutkan.\n" + "Tetap pinjamkan?",
                            "Mobil Rusak",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Warning,
                            MessageBoxDefaultButton.Button2); //"No" is the default, so a stray Enter cancels

                        if (answer != DialogResult.Yes) return;
                    }
                    rentAsCashier.Start(car);
                    ShowPage(rentAsCashier);
                };
            }
            else
            {
                borrow.Text = car.Status == "dipinjam" ? "Sedang Dipinjam" : "Tidak Tersedia";
                Theme.StyleDisabled(borrow);
            }

            card.Controls.Add(details);
            card.Controls.Add(nameRow);
            card.Controls.Add(picture);
            card.Controls.Add(borrow);

            carGrid.Controls.Add(card);
        }

        carGrid.ResumeLayout();
    }
    
}
