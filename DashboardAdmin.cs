using MySqlConnector;

namespace Rentalin;

public class DashboardAdmin : DashboardBase
{
    readonly Navbar nav;
    readonly DataPage carsPage;
    readonly DataPage usersPage;

    public DashboardAdmin(string name)
    {
        AutoScaleMode = AutoScaleMode.Font;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = true;
        WindowState = FormWindowState.Maximized;
        Text = "Dashboard Admin | " + name;
        BackColor = Theme.Background;
        Font = new Font("Segoe UI", 10f);
        ForeColor = Theme.TextDark;

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 110)); //navbar
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); //content

        nav = new Navbar(name, "Mobil", "Pengguna");
        nav.ItemClicked += ShowPage;
        nav.LogoutClicked += () => { LoggedOut = true; Close(); };

        carsPage = new DataPage("Cari mobil...");
        usersPage = new DataPage("Cari pengguna...") { Visible = false };

        // placeholders until the forms exist
        carsPage.AddRequested += () => MessageBox.Show("Tambah mobil: TBA");
        carsPage.EditRequested += id => MessageBox.Show($"Edit mobil #{id}: TBA");
        carsPage.DeleteRequested += id => MessageBox.Show($"Hapus mobil #{id}: TBA");
        usersPage.AddRequested += () => MessageBox.Show("Tambah pengguna: TBA");
        usersPage.EditRequested += id => MessageBox.Show($"Edit pengguna #{id}: TBA");
        usersPage.DeleteRequested += id => MessageBox.Show($"Hapus pengguna #{id}: TBA");

        var content = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20, 15, 20, 20) };
        content.Controls.Add(carsPage);
        content.Controls.Add(usersPage);

        root.Controls.Add(nav, 0, 0);
        root.Controls.Add(content, 0, 1);
        Controls.Add(root);

        ShowPage(0);
        Shown += (s, e) => { LoadCars(); LoadUsers(); };
    }

    void ShowPage(int index)
    {
        carsPage.Visible = index == 0;
        usersPage.Visible = index == 1;
        nav.SetActive(index);
    }

    void LoadCars()
    {
        try
        {
            carsPage.SetData(Db.Query(
                @"SELECT id, name AS Nama, color AS Warna, seats AS Kursi, plate_num AS Plat,
                         price AS Harga, IF(damaged, 'Rusak', '-') AS Kondisi, status AS Status
                  FROM cars ORDER BY name"));
            carsPage.FormatColumn("Harga", "'Rp'#,##0");
        }
        catch (MySqlException ex)
        {
            MessageBox.Show("Gagal memuat data mobil:\n" + ex.Message);
        }
    }

    void LoadUsers()
    {
        try
        {
            usersPage.SetData(Db.Query(
                @"SELECT id, username AS Username, name AS Nama, role AS Role
                  FROM users ORDER BY role, name"));
        }
        catch (MySqlException ex)
        {
            MessageBox.Show("Gagal memuat data pengguna:\n" + ex.Message);
        }
    }
}