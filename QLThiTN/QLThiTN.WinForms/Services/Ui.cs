using System.Data;

namespace QLThiTN.WinForms.Services;

/// <summary>Bang mau + trinh giup dung giao dien thong nhat voi nhan dien website.</summary>
public static class Ui
{
    public static readonly Color Navy = Color.FromArgb(27, 42, 78);
    public static readonly Color NavyDark = Color.FromArgb(19, 31, 58);
    public static readonly Color Accent = Color.FromArgb(1, 180, 255);
    public static readonly Color Accent2 = Color.FromArgb(124, 77, 255);
    public static readonly Color BgSoft = Color.FromArgb(244, 247, 251);
    public static readonly Color BorderSoft = Color.FromArgb(230, 236, 245);
    public static readonly Color TextMuted = Color.FromArgb(108, 118, 147);
    public static readonly Color Success = Color.FromArgb(40, 167, 69);
    public static readonly Color Danger = Color.FromArgb(220, 53, 69);
    public static readonly Color Warning = Color.FromArgb(255, 159, 67);

    public static Font H1 = new("Segoe UI", 16f, FontStyle.Bold);
    public static Font H2 = new("Segoe UI", 12f, FontStyle.Bold);
    public static Font Body = new("Segoe UI", 9.5f);
    public static Font Small = new("Segoe UI", 8.5f);

    /// <summary>Don dep + trang tri DataGridView theo nhan dien chung.</summary>
    public static void StyleGrid(DataGridView grid)
    {
        grid.BorderStyle = BorderStyle.None;
        grid.BackgroundColor = Color.White;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.AllowUserToResizeRows = false;
        grid.ReadOnly = true;
        grid.RowHeadersVisible = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.MultiSelect = false;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.ColumnHeadersDefaultCellStyle.BackColor = Navy;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
        grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Navy;
        grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(6, 8, 6, 8);
        grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        grid.ColumnHeadersHeight = 40;
        grid.EnableHeadersVisualStyles = false;
        grid.DefaultCellStyle.Font = Body;
        grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(213, 242, 255);
        grid.DefaultCellStyle.SelectionForeColor = Navy;
        grid.DefaultCellStyle.Padding = new Padding(6, 4, 6, 4);
        grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(247, 250, 254);
        grid.GridColor = BorderSoft;
    }

    /// <summary>Dat ten cot kieu Viet cho grid tu bang TableName.ColumnName hoac cot tinh.</summary>
    public static void RenameColumns(DataGridView grid, params (string Col, string Title)[] titles)
    {
        foreach (var (col, title) in titles)
        {
            if (grid.Columns.Contains(col))
            {
                grid.Columns[col]!.HeaderText = title;
                grid.Columns[col]!.SortMode = DataGridViewColumnSortMode.NotSortable;
            }
        }
    }

    /// <summary>An cac cot ky thuat (ID...).</summary>
    public static void HideColumns(DataGridView grid, params string[] cols)
    {
        foreach (var c in cols)
            if (grid.Columns.Contains(c)) grid.Columns[c]!.Visible = false;
    }

    public static Button ButtonPrimary(string text, int w = 140, int h = 38)
    {
        var b = new Button
        {
            Text = text,
            Width = w,
            Height = h,
            FlatStyle = FlatStyle.Flat,
            BackColor = Navy,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false
        };
        b.FlatAppearance.BorderSize = 0;
        return b;
    }

    public static Button ButtonAccent(string text, int w = 140, int h = 38)
    {
        var b = new Button
        {
            Text = text,
            Width = w,
            Height = h,
            FlatStyle = FlatStyle.Flat,
            BackColor = Accent,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        b.FlatAppearance.BorderSize = 0;
        return b;
    }

    public static Button ButtonOutline(string text, int w = 140, int h = 38, Color? color = null)
    {
        var c = color ?? Navy;
        var b = new Button
        {
            Text = text,
            Width = w,
            Height = h,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.White,
            ForeColor = c,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        b.FlatAppearance.BorderColor = c;
        b.FlatAppearance.BorderSize = 1;
        return b;
    }

    public static Button ButtonDanger(string text, int w = 140, int h = 38) =>
        ButtonOutline(text, w, h, Danger);

    public static Label Label(string text, Font? font = null, Color? color = null)
    {
        return new Label
        {
            Text = text,
            AutoSize = true,
            Font = font ?? Body,
            ForeColor = color ?? Color.FromArgb(43, 52, 82)
        };
    }

    public static TextBox Input(int w = 260)
    {
        return new TextBox { Width = w, Font = Body, BorderStyle = BorderStyle.FixedSingle };
    }

    public static ComboBox Combo(int w = 260)
    {
        return new ComboBox
        {
            Width = w,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = Body,
            FlatStyle = FlatStyle.Flat
        };
    }

    /// <summary>Bang do mau cyan -> tim dung cho tieu de thanh phan (mo phong gradient web).</summary>
    public static void GradientButton(Button b, Color top, Color bottom)
    {
        b.FlatStyle = FlatStyle.Flat;
        b.FlatAppearance.BorderSize = 0;
        b.ForeColor = Color.White;
        b.UseVisualStyleBackColor = false;
        var owner = b;
        owner.Paint += (s, e) =>
        {
            using var brush = new System.Drawing.Drawing2D.LinearGradientBrush(owner.ClientRectangle, top, bottom, 90f);
            e.Graphics.FillRectangle(brush, owner.ClientRectangle);
            TextRenderer.DrawText(e.Graphics, owner.Text, owner.Font, owner.ClientRectangle, Color.White,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
        };
    }
}
