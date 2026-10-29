namespace Rentalin;

record RenterDocs(string KtpPath, string SimPath);

class DocumentSlot : TableLayoutPanel
{
    readonly PictureBox preview;
    readonly Button removeButton;
    string? path;

    public DocumentSlot(string title, string placeholder, Padding margin)
    {
        Dock = DockStyle.Fill;
        Margin = margin;
        ColumnCount = 1;
        RowCount = 3;
        ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        RowStyles.Add(new RowStyle(SizeType.Percent, 100)); 
        RowStyles.Add(new RowStyle(SizeType.Absolute, 56));

        var titleLabel = new Label {
            Dock = DockStyle.Fill,
            Text = title,
            ForeColor = Theme.TextMuted,
            TextAlign = ContentAlignment.BottomLeft
        };

        preview = new PictureBox {
            Dock = DockStyle.Fill,
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.FromArgb(238, 240, 243),
            Margin = new Padding(0, 6, 0, 8)
        };
        preview.Paint += (s, e) => {
            if (preview.Image != null) return;
            TextRenderer.DrawText(e.Graphics, placeholder, new Font("Segoe UI Semibold", 14f),
                preview.ClientRectangle, Theme.TextMuted,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        };

        var uploadButton = new Button { Text = "Upload", Dock = DockStyle.Fill, Margin = new Padding(0, 0, 8, 0) };
        Theme.StyleSecondary(uploadButton);
        uploadButton.Click += (s, e) => Upload();

        removeButton = new Button { Text = "Hapus", Dock = DockStyle.Fill, Margin = new Padding(8, 0, 0, 0) };
        Theme.StyleSecondary(removeButton);
        removeButton.Enabled = false;
        removeButton.Click += (s, e) => Clear();

        var buttons = new TableLayoutPanel {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0, 0, 0, 8)
        };
        buttons.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        buttons.Controls.Add(uploadButton, 0, 0);
        buttons.Controls.Add(removeButton, 1, 0);

        Controls.Add(titleLabel, 0, 0);
        Controls.Add(preview, 0, 1);
        Controls.Add(buttons, 0, 2);
    }

    void Upload()
    {
        using var dialog = new OpenFileDialog {
            Title = "Pilih foto",
            Filter = "Gambar|*.jpg;*.jpeg;*.png;*.bmp"
        };
        if (dialog.ShowDialog() != DialogResult.OK) return;

        try
        {
            using var fs = File.OpenRead(dialog.FileName);
            using var img = Image.FromStream(fs);
            var copy = new Bitmap(img);

            preview.Image?.Dispose();
            preview.Image = copy;
            path = dialog.FileName;
            removeButton.Enabled = true;
            preview.Invalidate();
        }
        catch (Exception ex) when (ex is ArgumentException or OutOfMemoryException or IOException)
        {
            MessageBox.Show("File bukan gambar yang valid.", "Gagal",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
    public string? GetPath() => path;
    public void Clear()
    {
        preview.Image?.Dispose();
        preview.Image = null;
        path = null;
        removeButton.Enabled = false;
        preview.Invalidate();
    }
}

class Documents : UserControl
{
    readonly DocumentSlot ktp;
    readonly DocumentSlot sim;

    public Documents()
    {
        Dock = DockStyle.Fill;

        var table = new TableLayoutPanel {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1
        };
        table.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        ktp = new DocumentSlot("Kartu Tanda Penduduk", "Foto KTP", new Padding(0, 0, 12, 0));
        sim = new DocumentSlot("Surat Izin Mengemudi", "Foto SIM", new Padding(12, 0, 0, 0));
        table.Controls.Add(ktp, 0, 0);
        table.Controls.Add(sim, 1, 0);

        Controls.Add(table);
    }

    public bool TryValidate(out string error)
    {
        if (ktp.GetPath() == null) { error = "Foto KTP wajib diunggah."; return false; }
        if (sim.GetPath() == null) { error = "Foto SIM wajib diunggah."; return false; }
        error = "";
        return true;
    }

    public RenterDocs GetDocuments() => new(ktp.GetPath()!, sim.GetPath()!);

    public void Reset()
    {
        ktp.Clear();
        sim.Clear();
    }
}