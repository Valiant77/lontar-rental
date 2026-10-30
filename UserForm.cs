using System.Text.RegularExpressions;
using MySqlConnector;

namespace Rentalin;

record RoleItem(string Label, string Value)
{
    public override string ToString() => Label;
}
class UserForm : Form
{
    readonly int? userId; //if null then add else then edit
    readonly TextBox nameBox;
    readonly TextBox usernameBox;
    readonly TextBox passwordBox;
    readonly ComboBox roleBox;

    static Label Lbl(string text) => new Label {
        Text = text,
        ForeColor = Theme.TextMuted,
        AutoSize = true,
        Margin = new Padding(0, 0, 0, 4)
    };
    static TextBox Box() => new TextBox {
        Dock = DockStyle.Fill,
        Font = new Font("Segoe UI", 11f),
        BorderStyle = BorderStyle.FixedSingle,
        Margin = new Padding(0, 0, 0, 14)
    };
    public UserForm(int? userId = null)
    {
        this.userId = userId;
        Text = userId == null ? "Tambah Pengguna" : "Edit Pengguna";
        AutoScaleMode = AutoScaleMode.Font;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Theme.Card;
        ForeColor = Theme.TextDark;
        Font = new Font("Segoe UI", 10f);
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;

        var root = new TableLayoutPanel {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            Padding = new Padding(24)
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 340));

        nameBox = Box();
        usernameBox = Box();
        passwordBox = Box();
        passwordBox.UseSystemPasswordChar = true;
        if (userId != null) passwordBox.PlaceholderText = "Kosongkan jika tidak diganti";

        roleBox = new ComboBox {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 11f),
            Margin = new Padding(0, 0, 0, 20)
        };
        roleBox.Items.Add(new RoleItem("Kasir", "cashier"));
        roleBox.Items.Add(new RoleItem("Admin", "admin"));
        roleBox.Items.Add(new RoleItem("Owner", "owner"));

        var saveButton = new Button { Text = "Simpan", Size = new Size(120, 44), Margin = new Padding(0) };
        Theme.StylePrimary(saveButton);
        saveButton.Click += (s, e) => Save();

        var cancelButton = new Button {
            Text = "Batal",
            Size = new Size(120, 44),
            Margin = new Padding(0, 0, 12, 0),
            DialogResult = DialogResult.Cancel
        };
        Theme.StyleSecondary(cancelButton);

        AcceptButton = saveButton;
        CancelButton = cancelButton;

        var buttons = new FlowLayoutPanel {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.RightToLeft,
            Margin = new Padding(0)
        };
        buttons.Controls.Add(saveButton);
        buttons.Controls.Add(cancelButton);

        root.Controls.Add(Lbl("Nama"));
        root.Controls.Add(nameBox);
        root.Controls.Add(Lbl("Username"));
        root.Controls.Add(usernameBox);
        root.Controls.Add(Lbl("Password"));
        root.Controls.Add(passwordBox);
        root.Controls.Add(Lbl("Role"));
        root.Controls.Add(roleBox);
        root.Controls.Add(buttons);
        Controls.Add(root);

        if (userId != null) LoadExisting(userId.Value);
        else roleBox.SelectedIndex = 0;

        Shown += (s, e) => nameBox.Focus();
    }
    void LoadExisting(int id)
    {
        try
        {
            using var conn = Db.Open();
            using var cmd = new MySqlCommand(
                "SELECT name, username, role FROM users WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("@id", id);
            using var reader = cmd.ExecuteReader();
            if (!reader.Read()) return;

            nameBox.Text = reader.GetString("name");
            usernameBox.Text = reader.GetString("username");

            string role = reader.GetString("role");
            foreach (RoleItem item in roleBox.Items)
                if (item.Value == role) roleBox.SelectedItem = item;
        }
        catch (MySqlException ex)
        {
            MessageBox.Show("Gagal memuat data pengguna:\n" + ex.Message);
        }
    }
    static void Warn(string message, Control focus)
    {
        MessageBox.Show(message, "Data belum lengkap", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        focus.Focus();
    }
    void Save()
    {
        string name = nameBox.Text.Trim();
        string username = usernameBox.Text.Trim();
        string password = passwordBox.Text;
        bool setPassword = password.Length > 0;

        if (name.Length < 2)
        { Warn("Nama wajib diisi.", nameBox); return; }
        if (!Regex.IsMatch(username, @"^[A-Za-z0-9_.]{3,32}$"))
        { Warn("Username 3 sampai 32 karakter: huruf, angka, titik atau garis bawah.", usernameBox); return; }
        if (userId == null && !setPassword)
        { Warn("Password wajib diisi.", passwordBox); return; }
        if (setPassword && password.Length < 8)
        { Warn("Password minimal 8 karakter.", passwordBox); return; }
        if (roleBox.SelectedItem is not RoleItem role)
        { Warn("Pilih role.", roleBox); return; }

        try
        {
            using var conn = Db.Open();

            string sql = userId == null
                ? "INSERT INTO users (name, username, password, role) VALUES (@n, @u, @p, @r)"
                : "UPDATE users SET name = @n, username = @u, role = @r" +
                  (setPassword ? ", password = @p" : "") + " WHERE id = @id";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@n", name);
            cmd.Parameters.AddWithValue("@u", username);
            cmd.Parameters.AddWithValue("@r", role.Value);
            if (setPassword)
                cmd.Parameters.AddWithValue("@p", BCrypt.Net.BCrypt.HashPassword(password));
            if (userId != null)
                cmd.Parameters.AddWithValue("@id", userId.Value);
            cmd.ExecuteNonQuery();

            //TODO: create the edit part

            DialogResult = DialogResult.OK;
        }
        catch (MySqlException ex) when (ex.ErrorCode == MySqlErrorCode.DuplicateKeyEntry)
        {
            Warn("Username sudah dipakai.", usernameBox);
        }
        catch (MySqlException ex)
        {
            MessageBox.Show("Gagal menyimpan:\n" + ex.Message);
        }
    }
}