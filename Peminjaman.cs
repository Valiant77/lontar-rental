namespace Rentalin;
//TODO: dont forget to replace these placeholders with the real deal

class BorrowPage : UserControl
{
    static readonly string[] StepTitles = {
        "Identitas Penyewa",
        "Foto KTP & SIM",
        "Ringkasan & Total"
    };

    readonly Label heading;
    readonly Label stepLabel;
    readonly Control[] steps;
    readonly Button backButton;
    readonly Button nextButton;
    int step;
    Car? car;

    public event Action? Finished;
    public event Action? Cancelled;

    public BorrowPage()
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
        steps = new Control[] {
            Placeholder("1. Identitas Penyewa\nTBA"),
            Placeholder("2. Foto KTP & SIM\nTBA"),
            Placeholder("3. Ringkasan & Total\nTBA")
        };
        foreach (var s in steps) body.Controls.Add(s);

        var cancelButton = new Button {
            Text = "Batal",
            Size = new Size(120, 44),
            Margin = new Padding(0, 8, 24, 8)
        };
        Theme.StyleSecondary(cancelButton);
        cancelButton.Click += (s, e) => Cancelled?.Invoke();

        backButton = new Button {
            Text = "<< Kembali",
            Size = new Size(140, 44),
            Margin = new Padding(0, 8, 12, 8)
        };
        Theme.StyleSecondary(backButton);
        backButton.Click += (s, e) => { step--; ShowStep(); };

        nextButton = new Button {
            Size = new Size(160, 44),
            Margin = new Padding(0, 8, 0, 8)
        };
        Theme.StylePrimary(nextButton);
        nextButton.Click += (s, e) => {
            if (step < steps.Length - 1)
            {
                step++;
                ShowStep();
            }
            else
            {
                //TODO: database: save the loan
                MessageBox.Show("Peminjaman disimpan (placeholder).");
                Finished?.Invoke();
            }
        };

        var buttons = new FlowLayoutPanel {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };
        buttons.Controls.Add(cancelButton);
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
        heading.Text = $"Peminjaman: {car.Name} ({car.Plate})";
        step = 0;
        ShowStep();
    }

    void ShowStep()
    {
        for (int i = 0; i < steps.Length; i++)
            steps[i].Visible = i == step;

        stepLabel.Text = $"Langkah {step + 1} dari {steps.Length} · {StepTitles[step]}";
        backButton.Visible = step > 0;
        nextButton.Text = step == steps.Length - 1 ? "Selesai" : "Lanjut >>";
    }
}