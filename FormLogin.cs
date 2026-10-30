using MySqlConnector;

namespace Rentalin;

public partial class FormLogin : Form
{
    public FormLogin()
    {
        //the window
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(960, 540);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Login LontarRental";
        BackColor = Theme.Background;
        Font = new Font("Segoe UI", 10f);
        ForeColor = Theme.TextDark;

        Image logoImage = Image.FromFile(Path.Combine(AppContext.BaseDirectory, "images", "flogo.png"));

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 1 };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var card = new TableLayoutPanel {
            BackColor = Theme.Card,
            Padding = new Padding(32),
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            Anchor = AnchorStyles.None
        };
        card.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 320));
        card.Paint += (s, e) => {
            using var pen = new Pen(Theme.Border);
            e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
        };

        var logo = new PictureBox {
            Image = logoImage,
            SizeMode = PictureBoxSizeMode.Zoom,
            Dock = DockStyle.Fill,
            Height = 100,
            Margin = new Padding(0, 0, 0, 24)
        };

        var usernameLabel = new Label {
            Text = "Username",
            ForeColor = Theme.TextMuted,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 4)
        };
        var usernameBox = new TextBox {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 11f),
            BorderStyle = BorderStyle.FixedSingle,
            Margin = new Padding(0, 0, 0, 16)
        };

        var passwordLabel = new Label {
            Text = "Password",
            ForeColor = Theme.TextMuted,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 4)
        };
        var passwordBox = new TextBox {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 11f),
            BorderStyle = BorderStyle.FixedSingle,
            UseSystemPasswordChar = true,
            Margin = new Padding(0, 0, 0, 24)
        };

        var loginButton = new Button {
            Text = "Login",
            Dock = DockStyle.Fill,
            Height = 44,
            Margin = new Padding(0)
        };
        Theme.StylePrimary(loginButton);
        AcceptButton = loginButton;

        card.Controls.Add(logo);
        card.Controls.Add(usernameLabel);
        card.Controls.Add(usernameBox);
        card.Controls.Add(passwordLabel);
        card.Controls.Add(passwordBox);
        card.Controls.Add(loginButton);
        root.Controls.Add(card, 0, 0);
        Controls.Add(root);

        loginButton.Click += (s, e) => {
            string username = usernameBox.Text.Trim();
            if (username == "" || passwordBox.Text == "")
            {
                MessageBox.Show("Lengkapi data terlebih dahulu", "Gagal");
                return;
            }
            try
            {
                using var conn = Db.Open();
                using var cmd = new MySqlCommand("SELECT id, name, password, role FROM users WHERE username = @u LIMIT 1", conn);
                cmd.Parameters.AddWithValue("@u", username);

                int userId;
                string name, hash, role;
                using (var reader = cmd.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        MessageBox.Show("Salah username atau password", "Gagal");
                        return;
                    }
                    userId = reader.GetInt32("id");
                    name = reader.GetString("name");
                    hash = reader.GetString("password");
                    role = reader.GetString("role");
                }
                bool ok;
                try
                {
                    ok = BCrypt.Net.BCrypt.Verify(passwordBox.Text, hash);
                }
                catch (BCrypt.Net.SaltParseException)
                {
                    ok = false;
                }

                if (!ok)
                {
                    MessageBox.Show("Salah username atau password", "Gagal");
                    return;
                }

                var dashboard = CreateDashboard(name, role);
                if (dashboard == null)
                {
                    MessageBox.Show("Role akun ini tidak dikenali", "Gagal");
                    return;
                }

                Session.Start(userId, name, role);
                Db.Log("login");

                dashboard.FormClosed += (s2, e2) => {
                    Session.End();
                    if (dashboard.LoggedOut)
                    {
                        usernameBox.Clear();
                        passwordBox.Clear();
                        Show();
                        usernameBox.Focus();
                    }
                    else Close();
                };
                dashboard.Show();
                Hide();
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Gagal meraih database:\n" + ex.Message);
            }
        };
    }

    //rbac
    static DashboardBase? CreateDashboard(string name, string role)
    {
        return role.Trim().ToLowerInvariant() switch {
            "cashier" => new DashboardCashier(name),
            "admin" => new DashboardAdmin(name),
            "owner" => new DashboardOwner(name),
            _ => null
        };
    }
}