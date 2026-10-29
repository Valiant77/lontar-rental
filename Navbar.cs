namespace Rentalin;

class Navbar : TableLayoutPanel
{
    readonly string displayName;
    readonly Label greeting;
    readonly List<Button> buttons = new();

    public event Action<int>? ItemClicked;
    public event Action? LogoutClicked;

    public Navbar(string displayName, params string[] labels)
    {
        this.displayName = displayName;
        Dock = DockStyle.Fill;
        ColumnCount = 4;
        RowCount = 1;
        BackColor = Theme.Card;
        Padding = new Padding(20, 0, 20, 0);
        RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120)); //logo
        ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 260)); //greeting
        ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); //items
        ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120)); //logout
        Paint += (s, e) => {
            using var pen = new Pen(Theme.Border);
            e.Graphics.DrawLine(pen, 0, Height - 1, Width, Height - 1);
        };

        var logo = new PictureBox {
            Image = Image.FromFile(Path.Combine(AppContext.BaseDirectory, "images", "clogo.png")),
            SizeMode = PictureBoxSizeMode.Zoom,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 8, 16, 8)
        };

        greeting = new Label {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 11f)
        };

        var items = new FlowLayoutPanel {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = false,
            Anchor = AnchorStyles.Left
        };
        for (int i = 0; i < labels.Length; i++)
        {
            int index = i;
            var b = new Button {
                Text = labels[i],
                Size = new Size(150, 44),
                Margin = new Padding(0, 0, 8, 0),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 10f),
                Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0;
            b.Click += (s, e) => ItemClicked?.Invoke(index);
            buttons.Add(b);
            items.Controls.Add(b);
        }

        var logout = new Button {
            Text = "Logout",
            Size = new Size(100, 44),
            Anchor = AnchorStyles.Right
        };
        Theme.StyleDanger(logout);
        logout.Click += (s, e) => LogoutClicked?.Invoke();

        Controls.Add(logo, 0, 0);
        Controls.Add(greeting, 1, 0);
        Controls.Add(items, 2, 0);
        Controls.Add(logout, 3, 0);

        UpdateGreeting();
        var timer = new System.Windows.Forms.Timer { Interval = 10_000 };
        timer.Tick += (s, e) => UpdateGreeting();
        timer.Start();
        Disposed += (s, e) => timer.Dispose();

        SetActive(0);
    }

    public void SetActive(int index)
    {
        for (int i = 0; i < buttons.Count; i++)
        {
            bool active = i == index;
            buttons[i].BackColor = active ? Theme.AccentSoft : Theme.Card;
            buttons[i].ForeColor = active ? Theme.Accent : Theme.TextMuted;
        }
    }

    void UpdateGreeting()
    {
        var now = DateTime.Now;
        string sapaan = now.Hour switch {
            < 12 => "Pagi",
            < 15 => "Siang",
            < 18 => "Sore",
            _ => "Malam"
        };
        greeting.Text = $"Selamat {sapaan}, {displayName}\n{now:dd/MM/yyyy, HH:mm}";
    }
}