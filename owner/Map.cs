using System.Drawing.Drawing2D;

namespace Rentalin;

class Map : Control
{
    List<MapPin> pins = new();
    public Map()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
        BackColor = Color.FromArgb(232, 236, 241);
    }
    public void SetPins(List<MapPin> newPins)
    {
        pins = newPins;
        Invalidate();
    }
    static double Fraction(string s, uint seed)
    {
        unchecked
        {
            uint h = 2166136261u ^ seed;
            foreach (char c in s) { h ^= c; h *= 16777619u; }
            return (h % 1000) / 1000.0;
        }
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        int w = Width, h = Height;

        // a few blocks and roads so it reads as a map
        using (var park = new SolidBrush(Color.FromArgb(214, 232, 214)))
            g.FillRectangle(park, w * 0.60f, h * 0.10f, w * 0.18f, h * 0.30f);
        using (var road = new Pen(Color.White, 12))
        {
            foreach (float y in new[] { 0.30f, 0.68f }) g.DrawLine(road, 0, h * y, w, h * y);
            foreach (float x in new[] { 0.20f, 0.55f, 0.82f }) g.DrawLine(road, w * x, 0, w * x, h);
            g.DrawLine(road, 0, h * 0.95f, w * 0.5f, h * 0.05f);
        }

        using var labelFont = new Font("Segoe UI", 8.5f);
        foreach (var pin in pins)
        {
            float x = (float)(w * (0.06 + 0.88 * Fraction(pin.Plate, 1)));
            float y = (float)(h * (0.10 + 0.80 * Fraction(pin.Plate, 2)));
            var color = pin.Overdue ? Theme.Danger : Theme.Accent;

            using (var fill = new SolidBrush(color))
            using (var border = new Pen(Color.White, 2))
            {
                g.FillEllipse(fill, x - 7, y - 7, 14, 14);
                g.DrawEllipse(border, x - 7, y - 7, 14, 14);
            }
            TextRenderer.DrawText(g, pin.Plate, labelFont, new Point((int)x + 10, (int)y - 9),
                Theme.TextDark, TextFormatFlags.NoPadding);
        }

        if (pins.Count == 0)
            TextRenderer.DrawText(g, "Tidak ada mobil yang sedang dipinjam", Font,
                ClientRectangle, Theme.TextMuted,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

        TextRenderer.DrawText(g, "*posisi bukan data asli, hanya dummy", labelFont,
            new Point(10, h - 22), Theme.TextMuted, TextFormatFlags.NoPadding);
    }
}