using System.Data;
using System.Globalization;
using System.Text;

namespace Rentalin;

class DataPage : UserControl
{
    static readonly CultureInfo Id = new("id-ID");

    readonly DataGridView grid;
    readonly TextBox searchBox;
    DataView? view;

    public event Action? AddRequested;
    public event Action<int>? EditRequested;
    public event Action<int>? DeleteRequested;

    public DataPage(string searchPlaceholder)
    {
        Dock = DockStyle.Fill;

        var layout = new TableLayoutPanel {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));    // search
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));    // table
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));    // buttons

        searchBox = new TextBox {
            PlaceholderText = searchPlaceholder,
            Width = 320,
            Font = new Font("Segoe UI", 11f),
            BorderStyle = BorderStyle.FixedSingle,
            Anchor = AnchorStyles.Left
        };
        searchBox.TextChanged += (s, e) => ApplyFilter();

        grid = new DataGridView {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            MultiSelect = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            RowHeadersVisible = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            BackgroundColor = Theme.Card,
            BorderStyle = BorderStyle.None,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            GridColor = Theme.Border,
            EnableHeadersVisualStyles = false,
            ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            ColumnHeadersHeight = 42
        };
        grid.RowTemplate.Height = 38;
        grid.ColumnHeadersDefaultCellStyle.BackColor = Theme.Background;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Theme.TextMuted;
        grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Theme.Background;
        grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = Theme.TextMuted;
        grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 10f);
        grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(8, 0, 8, 0);
        grid.DefaultCellStyle.BackColor = Theme.Card;
        grid.DefaultCellStyle.ForeColor = Theme.TextDark;
        grid.DefaultCellStyle.SelectionBackColor = Theme.AccentSoft;
        grid.DefaultCellStyle.SelectionForeColor = Theme.TextDark;
        grid.DefaultCellStyle.Padding = new Padding(8, 0, 8, 0);
        grid.CellDoubleClick += (s, e) => {
            if (e.RowIndex >= 0 && SelectedId() is int id) EditRequested?.Invoke(id);
        };

        var addButton = new Button { Text = "Tambah", Size = new Size(140, 44), Margin = new Padding(0, 10, 12, 10) };
        var editButton = new Button { Text = "Edit", Size = new Size(140, 44), Margin = new Padding(0, 10, 12, 10) };
        var deleteButton = new Button { Text = "Hapus", Size = new Size(140, 44), Margin = new Padding(0, 10, 0, 10) };
        Theme.StylePrimary(addButton);
        Theme.StyleSecondary(editButton);
        Theme.StyleDanger(deleteButton);

        addButton.Click += (s, e) => AddRequested?.Invoke();
        editButton.Click += (s, e) => {
            if (SelectedId() is int id) EditRequested?.Invoke(id); else NoSelection();
        };
        deleteButton.Click += (s, e) => {
            if (SelectedId() is int id) DeleteRequested?.Invoke(id); else NoSelection();
        };

        var buttons = new FlowLayoutPanel {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };
        buttons.Controls.Add(addButton);
        buttons.Controls.Add(editButton);
        buttons.Controls.Add(deleteButton);

        layout.Controls.Add(searchBox, 0, 0);
        layout.Controls.Add(grid, 0, 1);
        layout.Controls.Add(buttons, 0, 2);
        Controls.Add(layout);
    }

    // column named "id" is kept for lookups but hidden from the table
    public void SetData(DataTable table)
    {
        view = table.DefaultView;
        grid.DataSource = view;
        if (grid.Columns.Contains("id")) grid.Columns["id"]!.Visible = false;
        ApplyFilter();
    }

    public void FormatColumn(string column, string format)
    {
        if (!grid.Columns.Contains(column)) return;
        grid.Columns[column]!.DefaultCellStyle.Format = format;
        grid.Columns[column]!.DefaultCellStyle.FormatProvider = Id;
    }

    int? SelectedId()
    {
        if (grid.CurrentRow?.DataBoundItem is DataRowView row &&
            row.Row.Table.Columns.Contains("id"))
            return Convert.ToInt32(row["id"]);
        return null;
    }

    static void NoSelection() =>
        MessageBox.Show("Pilih salah satu baris terlebih dahulu.", "Belum ada pilihan",
            MessageBoxButtons.OK, MessageBoxIcon.Information);

    // live search across every visible column
    void ApplyFilter()
    {
        if (view == null) return;

        string text = searchBox.Text.Trim();
        if (text.Length == 0) { view.RowFilter = ""; return; }

        string esc = EscapeLike(text);
        var parts = view.Table!.Columns.Cast<DataColumn>()
            .Where(c => c.ColumnName != "id")
            .Select(c => $"Convert([{c.ColumnName}], 'System.String') LIKE '%{esc}%'");
        view.RowFilter = string.Join(" OR ", parts);
    }

    static string EscapeLike(string s)
    {
        var sb = new StringBuilder();
        foreach (char c in s)
        {
            if (c is '*' or '%' or '[' or ']') sb.Append('[').Append(c).Append(']');
            else if (c == '\'') sb.Append("''");
            else sb.Append(c);
        }
        return sb.ToString();
    }
}