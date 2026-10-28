using System.Globalization;

namespace Rentalin;

record Loan(string Invoice, string Vehicle, string Plate, string Borrower, string Birth, string Address, string Cashier, DateTime LoanDate, DateTime DueDate, decimal Total);

class ReturnPage : UserControl
{
    static readonly CultureInfo Id = new("id-ID");
    const decimal FinePerDay = 35_000m;

    // dummy data until the database is hooked up
    static readonly Dictionary<string, Loan> loans = new(StringComparer.OrdinalIgnoreCase) {
        ["ADRTH37678628"] = new("ADRTH37678628", "Blista Compact 2003, Biru", "SU4KM4D13",
            "Evelina Logunova", "Vorkuta-5, 19/06/1974",
            "Jalan Huniara, Danau Havasu, Arizona, Amerika Serikat",
            "Adolphe Plamondon", new DateTime(2026, 9, 23), new DateTime(2026, 10, 3), 277_500m),
        // not late yet, for testing the no-fine case
        ["ADRTH00000001"] = new("ADRTH00000001", "Obey Tailgater 1998, Biru", "B 4821 BTC",
            "Test Peminjam", "Jakarta, 01/01/1990", "Jalan Contoh No. 1, Jakarta",
            "Kasir3485", DateTime.Today.AddDays(-3), DateTime.Today.AddDays(2), 150_000m),
    };

    readonly TextBox invoiceBox;
    readonly Label errorLabel;
    readonly Label leftInfo;
    readonly Label rightInfo;
    readonly TableLayoutPanel details;
    readonly Button approveButton;
    Loan? current;

    public event Action? BackRequested;

    public ReturnPage()
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
                e.SuppressKeyPress = true;   // no "ding"
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
        if (!loans.TryGetValue(code, out var loan))
        {
            current = null;
            details.Visible = false;
            approveButton.Visible = false;
            errorLabel.Text = code.Length == 0 ? "" : "Invoice tidak ditemukan.";
            return;
        }

        current = loan;
        errorLabel.Text = "";

        var today = DateTime.Today;
        int late = Math.Max(0, (today - loan.DueDate.Date).Days);
        decimal fine = late * FinePerDay;

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

        // TODO: database: mark the loan as returned, make the car available, save the fine
        MessageBox.Show("Pengembalian berhasil disimpan.");
        Reset();
        BackRequested?.Invoke();
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