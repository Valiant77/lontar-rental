using System.Globalization;
using MySqlConnector;
namespace Rentalin;

record Loan(int Id, int CarId, string Invoice, string Vehicle, string Plate,
            string Borrower, string Birth, string Address, string Cashier,
            DateTime LoanDate, DateTime DueDate, int PricePerDay, int Total,
            DateTime? ReturnedAt);

class ReturnAsCashier : UserControl
{
    static readonly CultureInfo Id = new("id-ID");
    readonly TextBox invoiceBox;
    readonly Label errorLabel;
    readonly Label leftInfo;
    readonly Label rightInfo;
    readonly TableLayoutPanel details;
    readonly Button approveButton;
    Loan? current;

    public event Action? BackRequested;
    public event Action? Completed;

    public ReturnAsCashier()
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

        var layout = new TableLayoutPanel {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 6
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50)); //title
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30)); //kode invoice
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44)); //textbox
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34)); //error message
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); //details
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60)); //buttons

        var title = new Label {
            Dock = DockStyle.Fill,
            Text = "Pengembalian",
            Font = new Font("Segoe UI Semibold", 16f),
            TextAlign = ContentAlignment.MiddleLeft
        };

        var invoiceLabel = new Label {
            Dock = DockStyle.Fill,
            Text = "Kode Invoice",
            ForeColor = Theme.TextMuted,
            TextAlign = ContentAlignment.BottomLeft
        };
        invoiceBox = new TextBox {
            PlaceholderText = "Masukkan kode invoice lalu tekan Enter",
            Width = 400,
            Font = new Font("Segoe UI", 12f),
            BorderStyle = BorderStyle.FixedSingle,
            Anchor = AnchorStyles.Left
        };
        invoiceBox.KeyDown += (s, e) => {
            if (e.KeyCode == Keys.Enter) {
                e.SuppressKeyPress = true;   //no "ding"
                Lookup();
            }
        };

        errorLabel = new Label {
            Dock = DockStyle.Fill,
            ForeColor = Theme.Danger,
            TextAlign = ContentAlignment.MiddleLeft
        };

        details = new TableLayoutPanel {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Visible = false
        };
        details.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        details.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
        details.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));

        leftInfo = new Label { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 11f) };
        rightInfo = new Label { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 11f) };
        details.Controls.Add(leftInfo, 0, 0);
        details.Controls.Add(rightInfo, 1, 0);

        var backButton = new Button {
            Text = "<< Kembali",
            Size = new Size(160, 44),
            Margin = new Padding(0, 8, 12, 8)
        };
        Theme.StyleSecondary(backButton);
        backButton.Click += (s, e) => {
            Reset();
            BackRequested?.Invoke();
        };

        approveButton = new Button {
            Text = "Setujui Pengembalian",
            Size = new Size(200, 44),
            Margin = new Padding(0, 8, 0, 8),
            Visible = false
        };
        Theme.StylePrimary(approveButton);
        approveButton.Click += (s, e) => Approve();

        var buttons = new FlowLayoutPanel {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };
        buttons.Controls.Add(backButton);
        buttons.Controls.Add(approveButton);

        layout.Controls.Add(title, 0, 0);
        layout.Controls.Add(invoiceLabel, 0, 1);
        layout.Controls.Add(invoiceBox, 0, 2);
        layout.Controls.Add(errorLabel, 0, 3);
        layout.Controls.Add(details, 0, 4);
        layout.Controls.Add(buttons, 0, 5);
        card.Controls.Add(layout);
        Controls.Add(card);

        VisibleChanged += (s, e) => { if (Visible) invoiceBox.Focus(); };
    }
    static string Rp(decimal v) => "Rp" + v.ToString("N0", Id);
    void Lookup()
    {
        string code = invoiceBox.Text.Trim();

        current = null;
        details.Visible = false;
        approveButton.Visible = false;

        if (code.Length == 0) { errorLabel.Text = ""; return; }

        Loan? loan;
        try
        {
            loan = RentalService.Find(code);
        }
        catch (MySqlException ex)
        {
            errorLabel.Text = "Gagal membaca database: " + ex.Message;
            return;
        }

        if (loan == null) { errorLabel.Text = "Invoice tidak ditemukan."; return; }
        if (loan.ReturnedAt != null)
        {
            errorLabel.Text = $"Invoice ini sudah dikembalikan pada {loan.ReturnedAt:dd/MM/yyyy}.";
            return;
        }

        current = loan;
        errorLabel.Text = "";

        var today = DateTime.Today;
        int late = RentalService.LateDays(loan.DueDate, today);
        int fine = RentalService.Fine(loan.PricePerDay, late);

        leftInfo.Text =
            $"Kendaraan: {loan.Vehicle}\n" +
            $"Nomor Polisi: {loan.Plate}\n\n" +
            $"Nama Peminjam: {loan.Borrower}\n" +
            $"Tempat Tanggal Lahir: {loan.Birth}\n" +
            $"Alamat: {loan.Address}\n\n" +
            $"Nama Kasir: {loan.Cashier}\n" +
            $"Tgl. Peminjaman: {loan.LoanDate:dd/MM/yyyy}\n\n" +
            $"Total: {Rp(loan.Total)}";

        rightInfo.Text =
            $"Tgl. Pengembalian: {loan.DueDate:dd/MM/yyyy}\n" +
            $"Aktual: {today:dd/MM/yyyy}\n" +
            $"Keterlambatan: {late} Hari\n" +
            $"Denda: {Rp(fine)}";

        details.Visible = true;
        approveButton.Visible = true;
    }

    void Approve()
    {
        if (current == null) return;
        var answer = MessageBox.Show(
            $"Setujui pengembalian {current.Vehicle} ({current.Plate})?",
            "Konfirmasi", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (answer != DialogResult.Yes) return;

        try
        {
            int fine = RentalService.Return(current);
            MessageBox.Show($"Pengembalian berhasil disimpan.\nDenda: {Rp(fine)}", "Berhasil",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            Reset();
            Completed?.Invoke();
        }
        catch (RentalException ex)
        {
            MessageBox.Show(ex.Message, "Gagal", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (MySqlException ex)
        {
            MessageBox.Show("Gagal menyimpan ke database:\n" + ex.Message);
        }
    }

    void Reset()
    {
        current = null;
        invoiceBox.Clear();
        errorLabel.Text = "";
        details.Visible = false;
        approveButton.Visible = false;
    }
}