namespace Rentalin;
using System.Globalization;
using MySqlConnector;
using ScottPlot.WinForms;
class StatCard : Panel
{
    readonly Label valueLabel;

    public StatCard(string title)
    {
        Dock = DockStyle.Fill;
        Margin = new Padding(6);
        BackColor = Theme.Card;
        Padding = new Padding(18, 14, 18, 14);
        Paint += (s, e) => {
            using var pen = new Pen(Theme.Border);
            e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
        };

        valueLabel = new Label {
            Dock = DockStyle.Fill,
            Text = "–",
            Font = new Font("Segoe UI Semibold", 24f),
            TextAlign = ContentAlignment.MiddleLeft
        };
        var titleLabel = new Label {
            Dock = DockStyle.Top,
            Height = 26,
            Text = title,
            ForeColor = Theme.TextMuted
        };

        Controls.Add(valueLabel);
        Controls.Add(titleLabel);
    }

    public void SetValue(string text, Color? color = null)
    {
        valueLabel.Text = text;
        valueLabel.ForeColor = color ?? Theme.TextDark;
    }
}

class ChartCard : Panel
{
    readonly Panel body;
    readonly Label placeholder;

    public ChartCard(string title, string placeholderText, Control? headerExtra = null)
    {
        Dock = DockStyle.Fill;
        Margin = new Padding(6);
        BackColor = Theme.Card;
        Padding = new Padding(18, 14, 18, 14);
        Paint += (s, e) => {
            using var pen = new Pen(Theme.Border);
            e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
        };

        var header = new TableLayoutPanel {
            Dock = DockStyle.Top,
            Height = 40,
            ColumnCount = 2,
            RowCount = 1
        };
        header.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        header.Controls.Add(new Label {
            Dock = DockStyle.Fill,
            Text = title,
            Font = new Font("Segoe UI Semibold", 11f),
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);
        if (headerExtra != null)
        {
            headerExtra.Anchor = AnchorStyles.Right;
            header.Controls.Add(headerExtra, 1, 0);
        }

        placeholder = new Label {
            Dock = DockStyle.Fill,
            Text = placeholderText,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Theme.TextMuted,
            Font = new Font("Segoe UI Semibold", 12f)
        };
        body = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Background };
        body.Controls.Add(placeholder);

        Controls.Add(body);
        Controls.Add(header);
    }

    public Panel GetBody() => body;

    public void SetPlaceholder(string text) => placeholder.Text = text;
}

class OwnerOverview : UserControl
{
    static readonly CultureInfo Id = new("id-ID");
    static readonly string[] PieModes = { "Baru vs Lama", "Pendapatan per Mobil" };
    static readonly string[] Palette = { "#2563EB", "#F59E0B", "#10B981", "#8B5CF6", "#EC4899", "#94A3B8" };
    readonly StatCard transactionsCard;
    readonly StatCard incomeCard;
    readonly StatCard activeCard;
    readonly StatCard overdueCard;
    readonly ChartCard barCard;
    readonly ChartCard pieCard;
    readonly ChartCard topCarsCard;
    readonly ChartCard mapCard;
    readonly List<Button> pieButtons = new();
    readonly FormsPlot barPlot;
    readonly FormsPlot piePlot;
    readonly Label pieEmpty;
    readonly Label topCarsLabel;
    readonly Map map;
    Overview? data;
    int pieMode;
    bool errorShown;
    static string Rp(double v) => "Rp" + v.ToString("N0", Id);

    public OwnerOverview()
    {
        Dock = DockStyle.Fill;

        var grid = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 3 };
        for (int i = 0; i < 4; i++) grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 118));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

        //row 1: cards
        transactionsCard = new StatCard("Transaksi Hari Ini");
        incomeCard = new StatCard("Pendapatan Hari Ini");
        activeCard = new StatCard("Peminjaman Aktif");
        overdueCard = new StatCard("Terlambat");
        grid.Controls.Add(transactionsCard, 0, 0);
        grid.Controls.Add(incomeCard, 1, 0);
        grid.Controls.Add(activeCard, 2, 0);
        grid.Controls.Add(overdueCard, 3, 0);

        //row 2: bar chart + pie chart
        barCard = new ChartCard("Pendapatan 7 Hari Terakhir", "");
        barPlot = new FormsPlot();
        barPlot.UserInputProcessor.Disable();    //no pan/zoom; older 5.0 builds call this Interaction.Disable()
        Fill(barCard, barPlot);
        grid.Controls.Add(barCard, 0, 1);
        grid.SetColumnSpan(barCard, 2);

        var toggle = new FlowLayoutPanel {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = false,
            Margin = new Padding(0)
        };
        for (int i = 0; i < PieModes.Length; i++)
        {
            int mode = i;
            var b = new Button {
                Text = PieModes[i],
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(8, 2, 8, 2),
                Margin = new Padding(4, 4, 0, 4),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9f),
                Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0;
            b.Click += (s, e) => SetPieMode(mode);
            pieButtons.Add(b);
            toggle.Controls.Add(b);
        }
        pieCard = new ChartCard("Komposisi Minggu Ini", "", toggle);
        piePlot = new FormsPlot();
        piePlot.UserInputProcessor.Disable();
        Fill(pieCard, piePlot);
        pieEmpty = new Label {
            Dock = DockStyle.Fill,
            Text = "Belum ada data",
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Theme.TextMuted,
            BackColor = Theme.Card,
            Font = new Font("Segoe UI Semibold", 12f),
            Visible = false
        };
        pieCard.GetBody().Controls.Add(pieEmpty);   //covers the plot when there is nothing to draw
        grid.Controls.Add(pieCard, 2, 1);
        grid.SetColumnSpan(pieCard, 2);

        //row 3: top five + map
        topCarsCard = new ChartCard("Lima Mobil Terlaris (7 Hari)", "");
        topCarsLabel = new Label {
            Text = "",
            Font = new Font("Segoe UI", 10.5f),
            Padding = new Padding(8, 8, 0, 0)
        };
        Fill(topCarsCard, topCarsLabel);
        grid.Controls.Add(topCarsCard, 0, 2);

        mapCard = new ChartCard("Peta Pelacak (Demo)", "");
        map = new Map();
        Fill(mapCard, map);
        grid.Controls.Add(mapCard, 1, 2);
        grid.SetColumnSpan(mapCard, 3);

        Controls.Add(grid);
        SetPieMode(0);

        //reload whenever the tab is shown, and once a minute while it stays open
        VisibleChanged += (s, e) => { if (Visible) Reload(); };
        var timer = new System.Windows.Forms.Timer { Interval = 60_000 };
        timer.Tick += (s, e) => { if (Visible) Reload(); };
        timer.Start();
        Disposed += (s, e) => timer.Dispose();
    }

    static void Fill(ChartCard card, Control content)
    {
        var body = card.GetBody();
        body.Controls.Clear();
        body.BackColor = Theme.Card;
        content.Dock = DockStyle.Fill;
        body.Controls.Add(content);
    }

    public void Reload()
    {
        try
        {
            data = Stats.Load();
            errorShown = false;
        }
        catch (MySqlException ex)
        {
            if (!errorShown) //the timer would otherwise repeat this every minute
            {
                errorShown = true;
                MessageBox.Show("Gagal memuat dashboard:\n" + ex.Message);
            }
            return;
        }

        var o = data;
        var today = o.Week[^1];

        transactionsCard.SetValue(o.TodayRentals.ToString());
        incomeCard.SetValue(Rp(today.Rental + today.Fine));
        activeCard.SetValue(o.Active.ToString());
        overdueCard.SetValue(o.Overdue.ToString(), o.Overdue > 0 ? Theme.Danger : (Color?)null);

        DrawBars(o.Week);
        DrawPie();

        var lines = new List<string>();
        for (int i = 0; i < 5; i++)
            lines.Add(i < o.TopCars.Count
                ? $"{i + 1}. {o.TopCars[i].Name}\n    {o.TopCars[i].Plate} · {o.TopCars[i].Count}x"
                : $"{i + 1}. –");
        topCarsLabel.Text = string.Join("\n", lines);

        map.SetPins(o.Pins);
    }

    void DrawBars(List<DayIncome> days)
    {
        var plot = barPlot.Plot;
        plot.Clear();
        plot.FigureBackground.Color = ScottPlot.Color.FromHex("#FFFFFF");
        plot.DataBackground.Color = ScottPlot.Color.FromHex("#FFFFFF");

        //stacked: rental income in blue, fines on top in red
        var rental = new List<ScottPlot.Bar>();
        var fines = new List<ScottPlot.Bar>();
        var ticks = new ScottPlot.Tick[days.Count];
        for (int i = 0; i < days.Count; i++)
        {
            rental.Add(new ScottPlot.Bar {
                Position = i, ValueBase = 0, Value = days[i].Rental,
                FillColor = ScottPlot.Color.FromHex("#2563EB"), Size = 0.6
            });
            fines.Add(new ScottPlot.Bar {
                Position = i, ValueBase = days[i].Rental, Value = days[i].Rental + days[i].Fine,
                FillColor = ScottPlot.Color.FromHex("#DC2626"), Size = 0.6
            });
            ticks[i] = new ScottPlot.Tick(i, days[i].Day.ToString("ddd dd", Id));
        }
        plot.Add.Bars(rental).LegendText = "Sewa";
        plot.Add.Bars(fines).LegendText = "Denda";

        plot.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericManual(ticks);
        plot.Axes.Left.TickGenerator = new ScottPlot.TickGenerators.NumericAutomatic {
            LabelFormatter = v => v == 0 ? "0" : v >= 1_000_000 ? $"{v / 1_000_000:0.#} jt" : $"{v / 1_000:0} rb"
        };

        double top = Math.Max(100_000, days.Max(d => d.Rental + d.Fine) * 1.2);
        plot.Axes.SetLimitsX(-0.6, days.Count - 0.4);
        plot.Axes.SetLimitsY(0, top);
        plot.ShowLegend(ScottPlot.Alignment.UpperLeft);

        barPlot.Refresh();
    }

    void SetPieMode(int mode)
    {
        pieMode = mode;
        for (int i = 0; i < pieButtons.Count; i++)
        {
            bool active = i == mode;
            pieButtons[i].BackColor = active ? Theme.AccentSoft : Theme.Card;
            pieButtons[i].ForeColor = active ? Theme.Accent : Theme.TextMuted;
        }
        DrawPie();
    }

    void DrawPie()
    {
        if (data == null) return;

        var items = (pieMode == 0 ? data.PieNew : data.PieCars).Where(i => i.Value > 0).ToList();

        var plot = piePlot.Plot;
        plot.Clear();
        plot.FigureBackground.Color = ScottPlot.Color.FromHex("#FFFFFF");
        pieEmpty.Visible = items.Count == 0;

        if (items.Count > 0)
        {
            var slices = items.Select((it, i) => new ScottPlot.PieSlice {
                Value = it.Value,
                FillColor = ScottPlot.Color.FromHex(Palette[i % Palette.Length]),
                Label = pieMode == 0 ? $"{it.Label} ({it.Value:0})" : $"{it.Label}: {Rp(it.Value)}"
            }).ToList();

            plot.Add.Pie(slices);
            plot.Axes.Frameless();
            plot.HideGrid();
            plot.ShowLegend(ScottPlot.Alignment.MiddleRight);
        }
        piePlot.Refresh();
    }
}