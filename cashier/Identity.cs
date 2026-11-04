using System.Net.Mail;
using System.Text.RegularExpressions;
namespace Rentalin;
record Renter(string NIK, string Name, string Gender, string BirthPlace, DateTime BirthDate, string Phone, string Email, string Address);
class Identity : UserControl
{
    readonly TextBox NIKBox;
    readonly TextBox nameBox;
    readonly ComboBox genderBox;
    readonly TextBox birthPlaceBox;
    readonly DateTimePicker birthDateBox;
    readonly TextBox phoneBox;
    readonly TextBox emailBox;
    readonly TextBox addressBox;
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
    public Identity()
    {
        Dock = DockStyle.Fill;

        var table = new TableLayoutPanel {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 8
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15));
        for (int i = 0; i < 7; i++) table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 110));

        NIKBox = Box();
        NIKBox.MaxLength = 16;
        NIKBox.KeyPress += (s, e) => {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar)) e.Handled = true;
        };

        nameBox = Box();
        genderBox = new ComboBox {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Anchor = AnchorStyles.Left | AnchorStyles.Right,
            Font = new Font("Segoe UI", 11f),
            Margin = new Padding(0, 0, 16, 14)
        };
        genderBox.Items.AddRange(new object[] { "Laki-laki", "Perempuan" });
        birthPlaceBox = Box();
        birthDateBox = new DateTimePicker {
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "dd/MM/yyyy",
            MaxDate = DateTime.Today,
            Anchor = AnchorStyles.Left | AnchorStyles.Right,
            Font = new Font("Segoe UI", 11f),
            Margin = new Padding(0, 0, 16, 14)
        };
        phoneBox = Box();
        emailBox = Box();
        addressBox = Box();
        addressBox.Multiline = true;
        addressBox.Dock = DockStyle.Fill;
        addressBox.Margin = new Padding(0);

        // row 0-1: NIK
        table.Controls.Add(Lbl("NIK"), 0, 0);
        table.Controls.Add(NIKBox, 0, 1);

        // row 2-3: nama, gender, tempat lahir, tgl. lahir
        table.Controls.Add(Lbl("Nama"), 0, 2);
        table.Controls.Add(Lbl("Gender"), 1, 2);
        table.Controls.Add(Lbl("Tempat Lahir"), 2, 2);
        table.Controls.Add(Lbl("Tgl. Lahir"), 3, 2);
        table.Controls.Add(nameBox, 0, 3);
        table.Controls.Add(genderBox, 1, 3);
        table.Controls.Add(birthPlaceBox, 2, 3);
        table.Controls.Add(birthDateBox, 3, 3);

        // row 4-5: telepon, email
        var emailLabel = Lbl("Email");
        table.Controls.Add(Lbl("No. Telepon"), 0, 4);
        table.Controls.Add(emailLabel, 1, 4);
        table.SetColumnSpan(emailLabel, 3);
        table.Controls.Add(phoneBox, 0, 5);
        table.Controls.Add(emailBox, 1, 5);
        table.SetColumnSpan(emailBox, 3);

        // row 6-7: alamat
        var addressLabel = Lbl("Alamat");
        table.Controls.Add(addressLabel, 0, 6);
        table.SetColumnSpan(addressLabel, 4);
        table.Controls.Add(addressBox, 0, 7);
        table.SetColumnSpan(addressBox, 4);

        Controls.Add(table);

        // cursor starts in the first field whenever this step is shown
        VisibleChanged += (s, e) => { if (Visible) NIKBox.Focus(); };
    }
    static bool Fail(Control c, string msg, out string error)
    {
        error = msg;
        c.Focus();
        return false;
    }

    public bool TryValidate(out string error)
    {
        if (!Regex.IsMatch(NIKBox.Text, @"^\d{16}$"))
            return Fail(NIKBox, "NIK harus 16 digit angka.", out error);
        if (nameBox.Text.Trim().Length < 2)
            return Fail(nameBox, "Nama wajib diisi.", out error);
        if (genderBox.SelectedIndex < 0)
            return Fail(genderBox, "Pilih gender.", out error);
        if (birthPlaceBox.Text.Trim().Length == 0)
            return Fail(birthPlaceBox, "Tempat lahir wajib diisi.", out error);

        var born = birthDateBox.Value.Date;
        var today = DateTime.Today;
        int age = today.Year - born.Year;
        if (born > today.AddYears(-age)) age--;
        if (age < 17)
            return Fail(birthDateBox, "Peminjam harus berusia minimal 17 tahun (cek tanggal lahir).", out error);
        string phone = Regex.Replace(phoneBox.Text, @"[\s-]", "");
        if (!Regex.IsMatch(phone, @"^\+?\d{9,15}$"))
            return Fail(phoneBox, "Nomor telepon tidak valid.", out error);
        if (!MailAddress.TryCreate(emailBox.Text.Trim(), out _))
            return Fail(emailBox, "Format email tidak valid.", out error);
        if (addressBox.Text.Trim().Length < 5)
            return Fail(addressBox, "Alamat wajib diisi.", out error);

        error = "";
        return true;
    }
    public Renter GetRenter() => new(
        NIKBox.Text.Trim(),
        nameBox.Text.Trim(),
        genderBox.SelectedItem?.ToString() ?? "",
        birthPlaceBox.Text.Trim(),
        birthDateBox.Value.Date,
        Regex.Replace(phoneBox.Text, @"[\s-]", ""),
        emailBox.Text.Trim(),
        addressBox.Text.Trim());

    public void Reset()
    {
        NIKBox.Clear();
        nameBox.Clear();
        birthPlaceBox.Clear();
        phoneBox.Clear();
        emailBox.Clear();
        addressBox.Clear();
        genderBox.SelectedIndex = -1;
        birthDateBox.Value = DateTime.Today;
    }
}