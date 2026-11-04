using System.Globalization;

namespace Rentalin;

record LoanTerms(DateTime LoanDate, DateTime ReturnDate, int Days, decimal Subtotal, decimal Tax, decimal Total);

class Summary : UserControl
{
    static readonly CultureInfo Id = new("id-ID");
    const decimal TaxRate = 0.11m;

    readonly Label infoLabel;
    readonly DateTimePicker loanDate;
    readonly DateTimePicker returnDate;
    readonly Label durationLabel;
    readonly Label subtotalValue;
    readonly Label taxValue;
    readonly Label totalValue;
    Car? car;

    public event Action? FinishRequested;

    static string Rp(decimal v) => "Rp" + v.ToString("N0", Id);

    static Label Cell(string text, ContentAlignment align, Font? font = null) => new Label {
        Dock = DockStyle.Fill,
        Text = text,
        TextAlign = align,
        Font = font ?? new Font("Segoe UI", 11f)
    };

    public Summary()
    {
        Dock = DockStyle.Fill;

        var table = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        table.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));

        var left = new TableLayoutPanel {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Margin = new Padding(0, 0, 24, 0)
        };
        left.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        left.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); //details
        left.RowStyles.Add(new RowStyle(SizeType.AutoSize)); //date pickers
        left.RowStyles.Add(new RowStyle(SizeType.Absolute, 40)); //duration

        infoLabel = new Label { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 11f) };

        loanDate = new DateTimePicker {
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "dd/MM/yyyy",
            Anchor = AnchorStyles.Left | AnchorStyles.Right,
            Font = new Font("Segoe UI", 11f),
            Margin = new Padding(0, 0, 12, 0)
        };
        returnDate = new DateTimePicker {
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "dd/MM/yyyy",
            Anchor = AnchorStyles.Left | AnchorStyles.Right,
            Font = new Font("Segoe UI", 11f),
            Margin = new Padding(12, 0, 0, 0)
        };
        loanDate.ValueChanged += (s, e) => SyncDates();
        returnDate.ValueChanged += (s, e) => Recalculate();

        var dates = new TableLayoutPanel {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2,
            RowCount = 2
        };
        dates.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        dates.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        var l1 = new Label { Text = "Tgl. Peminjaman", ForeColor = Theme.TextMuted, AutoSize = true, Margin = new Padding(0, 8, 0, 4) };
        var l2 = new Label { Text = "Tgl. Pengembalian", ForeColor = Theme.TextMuted, AutoSize = true, Margin = new Padding(12, 8, 0, 4) };
        dates.Controls.Add(l1, 0, 0);
        dates.Controls.Add(l2, 1, 0);
        dates.Controls.Add(loanDate, 0, 1);
        dates.Controls.Add(returnDate, 1, 1);

        durationLabel = Cell("", ContentAlignment.MiddleLeft);
        durationLabel.ForeColor = Theme.TextMuted;

        left.Controls.Add(infoLabel, 0, 0);
        left.Controls.Add(dates, 0, 1);
        left.Controls.Add(durationLabel, 0, 2);

        var right = new TableLayoutPanel {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Margin = new Padding(24, 0, 0, 0)
        };
        right.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        right.RowStyles.Add(new RowStyle(SizeType.AutoSize)); //totals
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 64)); //print buttons
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 64)); //finish
        right.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); //note

        var totals = new TableLayoutPanel {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2,
            RowCount = 3
        };
        totals.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        totals.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        for (int i = 0; i < 3; i++) totals.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));

        var bold = new Font("Segoe UI Semibold", 13f);
        subtotalValue = Cell("", ContentAlignment.MiddleRight);
        taxValue = Cell("", ContentAlignment.MiddleRight);
        totalValue = Cell("", ContentAlignment.MiddleRight, bold);
        totalValue.ForeColor = Theme.Accent;

        totals.Controls.Add(Cell("Subtotal:", ContentAlignment.MiddleLeft), 0, 0);
        totals.Controls.Add(subtotalValue, 1, 0);
        totals.Controls.Add(Cell("Pajak (11%):", ContentAlignment.MiddleLeft), 0, 1);
        totals.Controls.Add(taxValue, 1, 1);
        totals.Controls.Add(Cell("Total:", ContentAlignment.MiddleLeft, bold), 0, 2);
        totals.Controls.Add(totalValue, 1, 2);

        var printReceipt = new Button { Text = "Print Bukti Peminjaman", Dock = DockStyle.Fill, Margin = new Padding(0, 8, 6, 8) };
        var printInvoice = new Button { Text = "Print Invoice", Dock = DockStyle.Fill, Margin = new Padding(6, 8, 0, 8) };
        Theme.StyleSecondary(printReceipt);
        Theme.StyleSecondary(printInvoice);
        //TODO: real printing (PrintDocument)
        printReceipt.Click += (s, e) => MessageBox.Show("Print bukti peminjaman: TBA");
        printInvoice.Click += (s, e) => MessageBox.Show("Print invoice: TBA");

        var printRow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        printRow.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        printRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        printRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        printRow.Controls.Add(printReceipt, 0, 0);
        printRow.Controls.Add(printInvoice, 1, 0);

        var finishButton = new Button {
            Text = "Selesaikan Transaksi",
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 8, 0, 8)
        };
        Theme.StylePrimary(finishButton);
        finishButton.Click += (s, e) => {
            var answer = MessageBox.Show(
                "Pastikan semua data sudah benar dan bukti peminjaman sudah ditandatangani.\n\nSelesaikan transaksi?",
                "Konfirmasi", MessageBoxButtons.YesNo, MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);
            if (answer == DialogResult.Yes) FinishRequested?.Invoke();
        };

        var note = new Label {
            Dock = DockStyle.Fill,
            Text = "*Pastikan semua data sudah benar dan bukti peminjaman sudah ditandatangani",
            ForeColor = Theme.TextMuted,
            Padding = new Padding(0, 4, 0, 0)
        };

        right.Controls.Add(totals, 0, 0);
        right.Controls.Add(printRow, 0, 1);
        right.Controls.Add(finishButton, 0, 2);
        right.Controls.Add(note, 0, 3);

        table.Controls.Add(left, 0, 0);
        table.Controls.Add(right, 1, 0);
        Controls.Add(table);
    }

    public void Populate(Car car, Renter renter, string cashier)
    {
        this.car = car;

        infoLabel.Text =
            $"Kendaraan: {car.Name}, {car.Color}\n" +
            $"Nomor Polisi: {car.Plate}\n\n" +
            $"Nama Peminjam: {renter.Name}\n" +
            $"Tempat Tanggal Lahir: {renter.BirthPlace}, {renter.BirthDate:dd/MM/yyyy}\n" +
            $"Alamat: {renter.Address}\n" +
            $"No. Telepon: {renter.Phone}\n" +
            $"Email: {renter.Email}\n\n" +
            $"Nama Kasir: {cashier}";

        // loan date can't be in the past, return is at least a day later
        loanDate.MinDate = DateTimePicker.MinimumDateTime;
        loanDate.Value = DateTime.Today;
        loanDate.MinDate = DateTime.Today;

        returnDate.MinDate = DateTimePicker.MinimumDateTime;
        returnDate.Value = DateTime.Today.AddDays(1);
        SyncDates();
    }

    void SyncDates()
    {
        var min = loanDate.Value.Date.AddDays(1);
        returnDate.MinDate = DateTimePicker.MinimumDateTime;
        if (returnDate.Value.Date < min) returnDate.Value = min;
        returnDate.MinDate = min;
        Recalculate();
    }

    LoanTerms Terms()
    {
        int days = Math.Max(1, (returnDate.Value.Date - loanDate.Value.Date).Days);
        decimal subtotal = days * (car?.Price ?? 0);
        decimal tax = Math.Round(subtotal * TaxRate, 0);
        return new(loanDate.Value.Date, returnDate.Value.Date, days, subtotal, tax, subtotal + tax);
    }

    void Recalculate()
    {
        if (car == null) return;
        var t = Terms();
        durationLabel.Text = $"Lama sewa: {t.Days} hari × {Rp(car.Price)}";
        subtotalValue.Text = Rp(t.Subtotal);
        taxValue.Text = Rp(t.Tax);
        totalValue.Text = Rp(t.Total);
    }

    public LoanTerms GetTerms() => Terms();
}