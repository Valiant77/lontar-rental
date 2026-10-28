namespace Rentalin;
record Car(string Name, string Color, int Seats, string Plate, string? ImagePath = null);
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
}

public partial class DashboardKasir : DashboardBase
{
readonly string username;
    readonly Label greetingLabel;
    readonly FlowLayoutPanel carGrid;
    readonly Control carsPage;
    readonly ReturnPage returnPage;
    readonly BorrowPage borrowPage;
    readonly Button navCars;
    readonly Button navReturn;

    public DashboardKasir(string username) {
        this.username = username;
        AutoScaleMode = AutoScaleMode.Font;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = true;
        WindowState = FormWindowState.Maximized;
        Text = "Dashboard Kasir | " + username;
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
        borrowPage = new BorrowPage { Visible = false };
        borrowPage.Cancelled += () => ShowPage(carsPage);
        borrowPage.Finished += () => ShowPage(carsPage);
        //ALL PENGEMBALIAN STUFF
        returnPage = new ReturnPage { Visible = false };
        returnPage.BackRequested += () => ShowPage(carsPage);

        var content = new Panel {
            Dock = DockStyle.Fill,
            Padding = new Padding(20, 15, 20, 20)
        };
        content.Controls.Add(carsPage);
        content.Controls.Add(borrowPage);
        content.Controls.Add(returnPage);

        navCars.Click += (s, e) => ShowPage(carsPage);
        navReturn.Click += (s, e) => ShowPage(returnPage);
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

        var cars = new List<Car> {
            new("Blista Compact 2003", "Biru", 4, $"B {Random.Shared.Next(1000, 10000)} BTC"),
            new("Obey Tailgater 1998", "Biru", 4, $"B {Random.Shared.Next(1000, 10000)} BTC"),
            new("Modo Dominico 2014", "Merah", 4, $"B {Random.Shared.Next(1000, 10000)} BTC"),
            new("Baloney X845", "Hitam", 4, $"B {Random.Shared.Next(1000, 10000)} BTC"),
            new("Osterreich Classic", "Putih", 4, $"B {Random.Shared.Next(1000, 10000)} BTC"),
            new("Caperoni Pecorino", "Biru", 4, $"B {Random.Shared.Next(1000, 10000)} BTC"),
            new("Blista Compact 2003", "Biru", 4, $"B {Random.Shared.Next(1000, 10000)} BTC"),
            new("Obey Tailgater 1998", "Biru", 4, $"B {Random.Shared.Next(1000, 10000)} BTC"),
            new("Modo Dominico 2014", "Merah", 4, $"B {Random.Shared.Next(1000, 10000)} BTC"),
            new("Baloney X845", "Hitam", 4, $"B {Random.Shared.Next(1000, 10000)} BTC"),
            new("Osterreich Classic", "Putih", 4, $"B {Random.Shared.Next(1000, 10000)} BTC"),
            new("Caperoni Pecorino", "Biru", 4, $"B {Random.Shared.Next(1000, 10000)} BTC"),
        };
        Shown += (s, e) => RenderCars(cars);
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
        returnPage.Visible = page == returnPage;
        borrowPage.Visible = page == borrowPage;
        SetActive(navCars, page == carsPage || page == borrowPage);
        SetActive(navReturn, page == returnPage);
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
        greetingLabel.Text = $"Selamat {sapaan}, {username}\n{now:dd/MM/yyyy, HH:mm}";
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
            if (car.ImagePath != null && File.Exists(car.ImagePath))
                picture.Image = Image.FromFile(car.ImagePath);

            var name = new Label {
                Dock = DockStyle.Top,
                Height = 34,
                Text = car.Name,
                Font = new Font("Segoe UI Semibold", 11f),
                AutoEllipsis = true,
                TextAlign = ContentAlignment.BottomLeft
            };

            var details = new Label {
                Dock = DockStyle.Fill,
                Text = $"{car.Color} | {car.Seats} Kursi\n{car.Plate}",
                ForeColor = Theme.TextMuted,
                Padding = new Padding(0, 4, 0, 0)
            };

            var borrow = new Button {
                Dock = DockStyle.Bottom,
                Height = 38,
                Text = "Pinjam"
            };
            Theme.StylePrimary(borrow);
            borrow.Click += (s, e) => {
                borrowPage.Start(car);
                ShowPage(borrowPage);
            };

            card.Controls.Add(details);
            card.Controls.Add(name);
            card.Controls.Add(picture);
            card.Controls.Add(borrow);

            carGrid.Controls.Add(card);
        }

        carGrid.ResumeLayout();
    }
    
}
