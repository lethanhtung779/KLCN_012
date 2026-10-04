namespace QLThiTN.WinForms.Controls;

/// <summary>
/// Bieu do cot don gian tu ve (khong phu thuoc thu vien chart ben ngoai):
/// moi cot = 1 nhan + gia tri; ti le mau theo do cao cot.
/// </summary>
public class BarChartControl : Control
{
    private List<(string Label, double Value)> _data = new();
    private double _maxValue;
    private string _unit = "";

    public BarChartControl()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
        BackColor = Color.White;
        Font = new Font("Segoe UI", 9f);
    }

    public void SetData(IEnumerable<(string Label, double Value)> data, string unit = "")
    {
        _data = data.ToList();
        _unit = unit;
        _maxValue = Math.Max(1, _data.Count > 0 ? _data.Max(d => d.Value) : 1);
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        if (_data.Count == 0)
        {
            TextRenderer.DrawText(g, "Không có dữ liệu", Font, ClientRectangle,
                Color.FromArgb(108, 118, 147), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            return;
        }

        var padTop = 28;
        var padBottom = 44;
        var padLeft = 40;
        var padRight = 12;
        var w = Math.Max(1, Width - padLeft - padRight);
        var h = Math.Max(1, Height - padTop - padBottom);

        // tru cot + luoi ngang
        using var gridPen = new Pen(Color.FromArgb(230, 236, 245), 1f);
        for (var i = 0; i <= 5; i++)
        {
            var y = padTop + h * i / 5;
            g.DrawLine(gridPen, padLeft, y, Width - padRight, y);
            var label = (_maxValue * (5 - i) / 5).ToString("0.#");
            TextRenderer.DrawText(g, label, new Font(Font, FontStyle.Regular),
                new Rectangle(0, (int)y - 8, padLeft - 6, 16),
                Color.FromArgb(108, 118, 147), TextFormatFlags.Right);
        }

        var slot = (double)w / _data.Count;
        var barWidth = Math.Max(8, (int)(slot * 0.62));
        var topColor = Color.FromArgb(1, 180, 255);
        var bottomColor = Color.FromArgb(124, 77, 255);

        for (var i = 0; i < _data.Count; i++)
        {
            var (label, value) = _data[i];
            var barHeight = (int)(h * value / _maxValue);
            var x = (int)(padLeft + slot * i + (slot - barWidth) / 2);
            var y = padTop + h - barHeight;

            using var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                new Rectangle(x, padTop, barWidth, h), topColor, bottomColor, 90f);
            g.FillRectangle(brush, x, y, barWidth, barHeight);

            // gia tri tren dinh cot
            var valText = value % 1 == 0 ? value.ToString("0") : value.ToString("0.#");
            TextRenderer.DrawText(g, valText + _unit, new Font(Font, FontStyle.Bold),
                new Rectangle(x - 14, Math.Max(0, y - 20), barWidth + 28, 18),
                Color.FromArgb(27, 42, 78), TextFormatFlags.HorizontalCenter);

            // nhan duoi cot (cat ngan neu dai)
            var labelText = label.Length > 14 ? label[..13] + "…" : label;
            TextRenderer.DrawText(g, labelText, Font,
                new Rectangle((int)(padLeft + slot * i) - 10, padTop + h + 6, (int)slot + 20, 34),
                Color.FromArgb(74, 84, 112),
                TextFormatFlags.HorizontalCenter | TextFormatFlags.WordBreak);
        }
    }
}
