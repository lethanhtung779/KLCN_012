using System.Globalization;
using QLThiTN.WinForms.Services;

namespace QLThiTN.WinForms.Forms.Admin;

public class frmLichHocLap : Form
{
    private readonly DateTimePicker _from = new() { Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy", Width = 135 };
    private readonly DateTimePicker _to = new() { Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy", Width = 135 };
    private readonly DateTimePicker _start = new() { Format = DateTimePickerFormat.Custom, CustomFormat = "HH:mm", ShowUpDown = true, Width = 90 };
    private readonly DateTimePicker _end = new() { Format = DateTimePickerFormat.Custom, CustomFormat = "HH:mm", ShowUpDown = true, Width = 90 };
    private readonly TextBox _room = Ui.Input(150);
    private readonly TextBox _content = Ui.Input(280);
    private readonly TextBox _holidays = Ui.Input(620);
    private readonly List<CheckBox> _days = [];
    private readonly DataGridView _grid = new() { Dock = DockStyle.Fill };
    private readonly Button _save = Ui.ButtonAccent("Lưu lịch", 130);
    private readonly Label _status = new() { Dock = DockStyle.Fill, AutoSize = false, ForeColor = Ui.Danger };
    private List<ScheduleOccurrence>? _preview;
    private bool _busy;

    public frmLichHocLap(int classId)
    {
        Text = "Tạo lịch học lặp theo tuần";
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(900, 700); MinimumSize = new Size(850, 650);
        Font = Ui.Body; BackColor = Color.White;
        _to.Value = DateTime.Today.AddMonths(3).AddDays(-1);
        _start.Value = DateTime.Today.AddHours(18);
        _end.Value = DateTime.Today.AddHours(20);
        _holidays.PlaceholderText = "Ngày nghỉ: dd/MM/yyyy; dd/MM/yyyy (có thể để trống)";
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), ColumnCount = 1, RowCount = 5 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        foreach (var h in new[] { 240, 48 }) layout.RowStyles.Add(new RowStyle(SizeType.Absolute, h));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        var input = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true };
        FlowLayoutPanel Row(params Control[] controls)
        {
            var row = new FlowLayoutPanel { Width = 795, Height = 40, WrapContents = false };
            row.Controls.AddRange(controls); input.Controls.Add(row); return row;
        }
        Label Label(string text) => new() { Text = text, AutoSize = true, Margin = new Padding(3, 7, 8, 0) };
        Row(Label("Từ ngày"), _from, Label("Đến ngày"), _to, Label("Giờ"), _start, Label("đến"), _end);
        var daysRow = Row(Label("Học vào"));
        for (var i = 0; i < 7; i++)
        {
            var day = (DayOfWeek)((i + 1) % 7);
            var box = new CheckBox { Text = i == 6 ? "Chủ nhật" : $"Thứ {i + 2}", Tag = day, AutoSize = true, Checked = i is 0 or 2 or 4 };
            box.CheckedChanged += (_, _) => InvalidatePreview();
            _days.Add(box); daysRow.Controls.Add(box);
        }
        Row(Label("Phòng"), _room, Label("Nội dung"), _content);
        Row(Label("Bỏ qua"), _holidays);
        var note = Label("Môn và giáo viên theo phân công của lớp. Ngày cuối được tính vào lịch.\nBuổi đã tạo vẫn có thể sửa hoặc xóa riêng trong lịch học.");
        note.MaximumSize = new Size(780, 0); input.Controls.Add(note);
        var previewButton = Ui.ButtonPrimary("Xem trước lịch", 180);
        previewButton.Click += async (_, _) =>
        {
            await Run(async () =>
            {
                InvalidatePreview();
                var holidays = _holidays.Text.Split([';', ',', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(s => DateTime.TryParseExact(s, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
                        ? date : throw new InvalidOperationException($"Ngày nghỉ không hợp lệ: {s}. Dùng dd/MM/yyyy."));
                var rows = RecurringScheduleService.Generate(_from.Value, _to.Value, _start.Value.TimeOfDay,
                    _end.Value.TimeOfDay, _days.Where(d => d.Checked).Select(d => (DayOfWeek)d.Tag!), holidays);
                var room = _room.Text;
                var errors = await Task.Run(() => RecurringScheduleService.Preview(classId, rows, room));
                _grid.DataSource = rows.Select((r, i) => new { Buoi = i + 1, BatDau = r.Start, KetThuc = r.End, TrangThai = errors[i].Length == 0 ? "Hợp lệ" : errors[i] }).ToList();
                Ui.RenameColumns(_grid, ("Buoi", "Buổi"), ("BatDau", "Bắt đầu"), ("KetThuc", "Kết thúc"), ("TrangThai", "Kiểm tra"));
                _grid.Columns["BatDau"].DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
                _grid.Columns["KetThuc"].DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
                for (var i = 0; i < errors.Count; i++)
                    if (errors[i].Length > 0) _grid.Rows[i].DefaultCellStyle.BackColor = Color.MistyRose;
                var conflicts = errors.Count(e => e.Length > 0);
                _preview = conflicts == 0 ? rows : null;
                _save.Enabled = _preview != null;
                _status.Text = $"{rows.Count} buổi dự kiến · {conflicts} buổi trùng lịch." +
                    (conflicts > 0 ? " Hãy đổi giờ/phòng hoặc thêm ngày trùng vào danh sách bỏ qua rồi xem trước lại." : " Có thể lưu toàn bộ lịch.");
            });
        };
        _save.Enabled = false;
        _save.Click += async (_, _) =>
        {
            if (_preview == null) return;
            var rows = _preview; var room = _room.Text; var content = _content.Text;
            await Run(async () =>
            {
                await Task.Run(() => RecurringScheduleService.Save(classId, rows, room, content));
                MessageBox.Show(this, $"Đã tạo {rows.Count} buổi học.", "Tạo lịch học");
                _busy = false;
                DialogResult = DialogResult.OK; Close();
            });
        };
        foreach (var date in new[] { _from, _to, _start, _end }) date.ValueChanged += (_, _) => InvalidatePreview();
        foreach (var text in new[] { _room, _content, _holidays }) text.TextChanged += (_, _) => InvalidatePreview();
        FormClosing += (_, e) => { if (_busy) e.Cancel = true; };
        Ui.StyleGrid(_grid);
        layout.Controls.Add(input, 0, 0); layout.Controls.Add(previewButton, 0, 1);
        layout.Controls.Add(_grid, 0, 2); layout.Controls.Add(_status, 0, 3); layout.Controls.Add(_save, 0, 4);
        Controls.Add(layout);

        async Task Run(Func<Task> action)
        {
            if (_busy) return;
            _busy = true; input.Enabled = previewButton.Enabled = _save.Enabled = false; UseWaitCursor = true;
            try { await action(); }
            catch (Exception ex) { _preview = null; _status.Text = ex.Message; }
            finally
            {
                _busy = false;
                if (!IsDisposed)
                {
                    input.Enabled = previewButton.Enabled = true; _save.Enabled = _preview != null; UseWaitCursor = false;
                    if (DialogResult == DialogResult.OK) Close();
                }
            }
        }
    }

    private void InvalidatePreview()
    {
        if (_busy) return;
        _preview = null; _save.Enabled = false; _grid.DataSource = null;
        _status.Text = "Bấm Xem trước lịch sau khi chọn ngày, thứ và giờ học.";
    }
}
