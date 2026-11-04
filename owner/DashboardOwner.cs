namespace Rentalin;

public class DashboardOwner : DashboardBase
{
    readonly Navbar nav;
    readonly Control[] pages;

    public DashboardOwner(string name)
    {
        AutoScaleMode = AutoScaleMode.Font;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = true;
        WindowState = FormWindowState.Maximized;
        Text = "Dashboard Owner | " + name;
        BackColor = Theme.Background;
        Font = new Font("Segoe UI", 10f);
        ForeColor = Theme.TextDark;

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 110));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        nav = new Navbar(name, "Dashboard", "Peminjaman", "Armada", "Riwayat", "Log");
        nav.ItemClicked += ShowPage;
        nav.LogoutClicked += () => { LoggedOut = true; Close(); };

        var overview = new OwnerOverview();
        pages = new Control[] {
            overview,
            Placeholder("Peminjaman\nTBA"),
            Placeholder("Armada\nTBA"),
            Placeholder("Riwayat\nTBA"),
            Placeholder("Log Aktivitas\nTBA")
        };

        var content = new Panel { Dock = DockStyle.Fill, Padding = new Padding(14, 9, 14, 14) };
        foreach (var p in pages) content.Controls.Add(p);

        root.Controls.Add(nav, 0, 0);
        root.Controls.Add(content, 0, 1);
        Controls.Add(root);

        ShowPage(0);
        Shown += (s, e) => overview.Reload();
    }

    static Label Placeholder(string text) => new Label {
        Dock = DockStyle.Fill,
        Text = text,
        TextAlign = ContentAlignment.MiddleCenter,
        Font = new Font("Segoe UI Semibold", 24f),
        ForeColor = Theme.TextMuted
    };

    void ShowPage(int index)
    {
        for (int i = 0; i < pages.Length; i++)
            pages[i].Visible = i == index;
        nav.SetActive(index);
    }
}