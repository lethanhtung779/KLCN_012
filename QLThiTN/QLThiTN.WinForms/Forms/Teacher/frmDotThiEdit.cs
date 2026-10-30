using QLThiTN.WinForms.Models;
using QLThiTN.WinForms.Services;

namespace QLThiTN.WinForms.Forms.Teacher;

public class frmDotThiEdit : Form
{
    private readonly ExamService _service = new();
    private readonly int _deThiId;
    private readonly string _tenDe;
    private readonly TextBox _name = Ui.Input(420);
    private readonly DateTimePicker _openDate = DatePicker();
    private readonly DateTimePicker _closeDate = DatePicker();
    private readonly DateTimePicker _openTime = TimePicker();
    private readonly DateTimePicker _closeTime = TimePicker();
    private readonly NumericUpDown _duration = new() { Minimum = 1, Maximum = 300, Value = 45, Font = Ui.Body };
    private readonly NumericUpDown _capacity = new() { Minimum = 0, Maximum = 1000, Font = Ui.Body };
    private readonly NumericUpDown _attempts = new() { Minimum = 1, Maximum = 10, Value = 1, Font = Ui.Body };
    private readonly ComboBox _window = Ui.Combo(300);
    private readonly ComboBox _scope = Ui.Combo(300);
    private readonly CheckBox _publish = new() { Text = "Công bố điểm ngay khi nộp bài", AutoSize = true, Font = Ui.Body, Checked = true };
    private readonly CheckedListBox _classes = new() { Height = 140, CheckOnClick = true, Font = Ui.Body, HorizontalScrollbar = true };
    private readonly Label _error = Ui.Label("", Ui.Small, Ui.Danger);
    private bool _settingWindow;
    private DateTime Opens => _openDate.Value.Date.AddHours(_openTime.Value.Hour).AddMinutes(_openTime.Value.Minute);
    private DateTime Closes => _closeDate.Value.Date.AddHours(_closeTime.Value.Hour).AddMinutes(_closeTime.Value.Minute);

    public frmDotThiEdit(int deThiId, string tenDe)
    {
        _deThiId = deThiId; _tenDe = tenDe;
        Text = "Tạo lịch thi cho đề";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(780, 620); ClientSize = new Size(860, 760);
        BackColor = Color.White; Font = Ui.Body;
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));
        var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        var fields = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2, Padding = new Padding(20) };
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        scroll.Controls.Add(fields); root.Controls.Add(scroll, 0, 0);
        void Add(Control control, int col, int row, bool span = false)
        { fields.Controls.Add(control, col, row); if (span) fields.SetColumnSpan(control, 2); }
        var title = Ui.Label("Đang lên lịch cho đề: " + tenDe, Ui.H2, Ui.Navy);
        title.MaximumSize = new Size(720, 0); Add(title, 0, 0, true);
        _name.Text = "Lịch thi - " + tenDe;
        Add(Field("Tên lịch thi *", _name), 0, 1, true);
        Add(Field("Mở cổng · chọn ngày và giờ", DateTimeInput(_openDate, _openTime)), 0, 2);
        Add(Field("Đóng cổng · chọn ngày và giờ", DateTimeInput(_closeDate, _closeTime)), 1, 2);
        Add(Field("Thời lượng mỗi bài (phút)", _duration), 0, 3);
        _window.Items.AddRange(["Bằng thời lượng bài", "Sau 1 giờ", "Sau 1 ngày", "Sau 7 ngày", "Tự chọn ngày / giờ đóng"]);
        Add(Field("Đặt nhanh thời gian đóng cổng", _window), 1, 3);
        Add(Field("Số lần thi tối đa", _attempts), 0, 4);
        Add(Field("Số chỗ (0 = không giới hạn)", _capacity), 1, 4);
        _scope.Items.AddRange(["Toàn trung tâm — mọi học viên đủ điều kiện", "Theo lớp — chỉ các lớp được chọn"]);
        _scope.SelectedIndex = 0; _scope.DropDownWidth = 450;
        Add(Field("Phạm vi dự thi", _scope), 0, 5);
        Add(Field("Kết quả", _publish), 1, 5);
        var note = Ui.Label("Khoảng mở cổng là thời gian được phép vào thi. Thời lượng là số phút mỗi người làm bài; đến giờ đóng cổng bài vẫn hết hạn. Bỏ chọn công bố điểm: điểm được công bố khi đóng cổng hoặc admin công bố sớm.", Ui.Small, Ui.TextMuted);
        note.MaximumSize = new Size(720, 0); Add(note, 0, 6, true);
        Add(Field("Lớp dự thi (bắt buộc chọn khi dùng phạm vi Theo lớp)", _classes), 0, 7, true);
        foreach (System.Data.DataRow row in _service.LayLopChoDotThi(deThiId).Rows)
            _classes.Items.Add(new ClassOption(Convert.ToInt32(row["LopHocID"]), row["TenHienThi"].ToString() ?? ""));
        _classes.Enabled = false;
        _scope.SelectedIndexChanged += (_, _) => _classes.Enabled = _scope.SelectedIndex == 1;
        var footer = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(20, 0, 20, 6) };
        footer.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); footer.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        _error.Dock = DockStyle.Fill; _error.AutoSize = false; footer.Controls.Add(_error, 0, 0);
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
        var cancel = Ui.ButtonOutline("Hủy", 100, 38); cancel.DialogResult = DialogResult.Cancel;
        var create = Ui.ButtonAccent("Lưu lịch thi", 155, 38); create.Click += (_, _) => CreateSession();
        buttons.Controls.AddRange([cancel, create]); footer.Controls.Add(buttons, 0, 1);
        root.Controls.Add(footer, 0, 1); Controls.Add(root); AcceptButton = create; CancelButton = cancel;
        var now = DateTime.Now; _openDate.Value = now; _openTime.Value = now;
        _window.SelectedIndexChanged += (_, _) => ApplyWindow(); _window.SelectedIndex = 0;
        _openDate.ValueChanged += (_, _) => ApplyWindow(); _openTime.ValueChanged += (_, _) => ApplyWindow();
        _duration.ValueChanged += (_, _) => ApplyWindow();
        _closeDate.ValueChanged += (_, _) => { if (!_settingWindow) _window.SelectedIndex = 4; };
        _closeTime.ValueChanged += (_, _) => { if (!_settingWindow) _window.SelectedIndex = 4; };
    }
    private static DateTimePicker DatePicker() => new() { Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy", Font = Ui.Body, Dock = DockStyle.Fill };
    private static DateTimePicker TimePicker() => new() { Format = DateTimePickerFormat.Custom, CustomFormat = "HH:mm", ShowUpDown = true, Font = Ui.Body, Dock = DockStyle.Fill };
    private static Control DateTimeInput(Control date, Control time)
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Top, Height = 38, ColumnCount = 2 };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65)); panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));
        panel.Controls.Add(date, 0, 0); panel.Controls.Add(time, 1, 0); return panel;
    }
    private static Control Field(string text, Control input)
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 1, RowCount = 2, Margin = new Padding(0, 8, 16, 8) };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var label = Ui.Label(text, Ui.Body, Ui.Navy); label.Margin = new Padding(0, 0, 0, 6);
        panel.Controls.Add(label, 0, 0); input.Dock = DockStyle.Top; input.Margin = Padding.Empty;
        panel.Controls.Add(input, 0, 1); return panel;
    }
    private void ApplyWindow()
    {
        if (_window.SelectedIndex is < 0 or 4) return;
        var close = _window.SelectedIndex switch { 0 => Opens.AddMinutes((double)_duration.Value), 1 => Opens.AddHours(1), 2 => Opens.AddDays(1), _ => Opens.AddDays(7) };
        _settingWindow = true;
        try { _closeDate.Value = close; _closeTime.Value = close; } finally { _settingWindow = false; }
    }
    private sealed record ClassOption(int Id, string Name) { public override string ToString() => Name; }
    private void CreateSession()
    {
        _error.Text = "";
        try
        {
            if (Closes <= Opens) { _error.Text = "Giờ đóng cổng phải sau giờ mở cổng."; return; }
            var dot = new DotThi { DeThiID = _deThiId, TenDotThi = _name.Text.Trim(), TenDe = _tenDe,
                ThoiGianMoCong = Opens, ThoiGianDongCong = Closes, ThoiLuongLamBai = (int)_duration.Value,
                GioiHanSoLuong = _capacity.Value > 0 ? (int)_capacity.Value : null,
                CongBoDiemSom = _publish.Checked, PhamVi = _scope.SelectedIndex == 1 ? "TheoLop" : "ToanTruong",
                SoLanThiToiDa = (int)_attempts.Value };
            var id = _service.TaoDotThi(dot, _classes.CheckedItems.Cast<ClassOption>().Select(x => x.Id).ToArray());
            MessageBox.Show(this, $"Đã tạo lịch thi #{id}.\nMở cổng: {Opens:dd/MM/yyyy HH:mm}\nĐóng cổng: {Closes:dd/MM/yyyy HH:mm}", "Hoàn tất");
            DialogResult = DialogResult.OK; Close();
        }
        catch (Exception ex) { _error.Text = ex.Message; }
    }
}
