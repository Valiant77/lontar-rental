namespace Rentalin;
using MySqlConnector;

class RentAsCashier : UserControl
{
    static readonly string[] StepTitles = {
        "Identitas Penyewa",
        "Dokumen KTP dan SIM",
        "Ringkasan dan Total"
    };

    readonly Identity identity;
    Renter? renter;
    readonly Documents documents;
    RenterDocs? docs;
    readonly Summary summary;
    readonly string cashierName;
    readonly Label heading;
    readonly Label stepLabel;
    readonly Control[] steps;
    readonly Button backButton;
    readonly Button nextButton;
    int step;
    Car? car;

    public event Action? Finished;
    public event Action? Cancelled;

    public RentAsCashier(string cashierName)
    {
        this.cashierName = cashierName;
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
            RowCount = 4
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50)); //heading
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34)); //step indicator
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); //step content
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60)); //buttons

        heading = new Label {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI Semibold", 16f),
            TextAlign = ContentAlignment.MiddleLeft
        };

        stepLabel = new Label {
            Dock = DockStyle.Fill,
            ForeColor = Theme.Accent,
            Font = new Font("Segoe UI Semibold", 10f),
            TextAlign = ContentAlignment.MiddleLeft
        };

        var body = new Panel { Dock = DockStyle.Fill };
        identity = new Identity();
        documents = new Documents();
        summary = new Summary();
        summary.FinishRequested += OnFinish;
        steps = new Control[] { identity, documents, summary };
        foreach (var s in steps) body.Controls.Add(s);

        backButton = new Button {
            Text = "<< Kembali",
            Size = new Size(140, 44),
            Margin = new Padding(0, 8, 12, 8)
        };
        Theme.StyleSecondary(backButton);
        backButton.Click += (s, e) => {
            if (step == 0) Cancelled?.Invoke();
            else { step--; ShowStep(); }
        };

        nextButton = new Button {
            Size = new Size(160, 44),
            Margin = new Padding(0, 8, 0, 8)
        };
        Theme.StylePrimary(nextButton);
        nextButton.Click += (s, e) => {
            if (step == 0)
            {
                if (!identity.TryValidate(out var error))
                {
                    MessageBox.Show(error, "Data belum lengkap",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                renter = identity.GetRenter();
            }
            else if (step == 1)
            {
                if (!documents.TryValidate(out var error))
                {
                    MessageBox.Show(error, "Data belum lengkap",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                docs = documents.GetDocuments();
            }

            step++;
            if (steps[step] == summary)
                summary.Populate(car!, renter!, cashierName);
            ShowStep();
        };

        var buttons = new FlowLayoutPanel {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };
        buttons.Controls.Add(backButton);
        buttons.Controls.Add(nextButton);

        layout.Controls.Add(heading, 0, 0);
        layout.Controls.Add(stepLabel, 0, 1);
        layout.Controls.Add(body, 0, 2);
        layout.Controls.Add(buttons, 0, 3);
        card.Controls.Add(layout);
        Controls.Add(card);

        ShowStep();
    }

    static Label Placeholder(string text) => new Label {
        Dock = DockStyle.Fill,
        Text = text,
        TextAlign = ContentAlignment.MiddleCenter,
        Font = new Font("Segoe UI Semibold", 20f),
        ForeColor = Theme.TextMuted
    };

    public void Start(Car car)
    {
        this.car = car;
        renter = null;
        docs = null;
        identity.Reset();
        documents.Reset();
        step = 0;
        ShowStep();
    }
    void ShowStep()
    {
        for (int i = 0; i < steps.Length; i++)
            steps[i].Visible = i == step;

        heading.Text = $"Peminjaman Baru | {StepTitles[step]}";
        stepLabel.Text = $"Langkah {step + 1} dari {steps.Length}" + (car == null ? "" : $" · {car.Name} ({car.Plate})");
        nextButton.Visible = step < steps.Length - 1;
        nextButton.Text = "Lanjutkan >>";
    }
    void OnFinish()
    {
        if (car == null || renter == null || docs == null)
        {
            MessageBox.Show("Data peminjaman belum lengkap.", "Gagal");
            return;
        }

        try
        {
            string invoice = RentalService.Create(car, renter, docs, summary.GetTerms());
            MessageBox.Show($"Transaksi selesai.\nKode invoice: {invoice}", "Berhasil",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            Finished?.Invoke();   // the dashboard reloads the car list
        }
        catch (RentalException ex)
        {
            MessageBox.Show(ex.Message, "Gagal", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (MySqlException ex)
        {
            MessageBox.Show("Gagal menyimpan ke database:\n" + ex.Message);
        }
        catch (IOException ex)
        {
            MessageBox.Show("Gagal menyalin dokumen:\n" + ex.Message);
        }
    }
}