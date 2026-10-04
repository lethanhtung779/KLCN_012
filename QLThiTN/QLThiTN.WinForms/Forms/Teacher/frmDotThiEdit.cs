using QLThiTN.WinForms.Models;
using QLThiTN.WinForms.Services;

namespace QLThiTN.WinForms.Forms.Teacher;

/// <summary>Hop thoai tao dot thi: thoi gian, thoi luong, gioi han, cong bo diem...</summary>
public class frmDotThiEdit : Form
{
    private readonly ExamService _service = new();
    private readonly int _deThiId;
    private readonly string _tenDe;
    private readonly TextBox _txtTenDot = Ui.Input(420);
    private readonly DateTimePicker _dtpMo = new() { Width = 190, Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy HH:mm", Font = Ui.Body, ShowUpDown = true };
    private readonly DateTimePicker _dtpDong = new() { Width = 190, Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy HH:mm", Font = Ui.Body, ShowUpDown = true };
    private readonly NumericUpDown _numThoiLuong = new() { Width = 150, Minimum = 1, Maximum = 300, Value = 45, Font = Ui.Body };
    private readonly NumericUpDown _numGioiHan = new() { Width = 150, Minimum = 0, Maximum = 1000, Value = 0, Font = Ui.Body };
    private readonly NumericUpDown _numLanThi = new() { Width = 150, Minimum = 1, Maximum = 10, Value = 1, Font = Ui.Body };
    private readonly CheckBox _chkCongBo = new() { Text = "Công bố điểm ngay sau khi nộp (bỏ tick = chờ quản trị công bố)", AutoSize = true, Font = Ui.Body, Checked = true };
    private readonly ComboBox _cboPhamVi = Ui.Combo(190);
    private readonly Label _lblError = Ui.Label("", Ui.Small, Ui.Danger);

    public frmDotThiEdit(int deThiId, string tenDe)
    {
        _deThiId = deThiId;
        _tenDe = tenDe;

        Text = "Tạo đợt thi";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false; MinimizeBox = false;
        ClientSize = new Size(560, 470);
        BackColor = Color.White;

        var lblInfo = Ui.Label($"Đề thi: {tenDe}", Ui.H2, Ui.Accent2);
        var lblTen = Ui.Label("Tên đợt thi", Ui.H2, Ui.Navy);
        _txtTenDot.Text = $"Đợt thi - {tenDe}";

        var lblMo = Ui.Label("Thời gian mở công *", Ui.H2, Ui.Navy);
        _dtpMo.Value = DateTime.Now.AddMinutes(-5);

        var lblDong = Ui.Label("Thời gian đóng công *", Ui.H2, Ui.Navy);
        _dtpDong.Value = DateTime.Now.AddDays(7);

        var lblLuong = Ui.Label("Thời lượng làm bài (phút) *", Ui.H2, Ui.Navy);
        var lblHan = Ui.Label("Giới hạn số lượng (0 = không giới hạn)", Ui.H2, Ui.Navy);
        var lblLan = Ui.Label("Số lần thi tối đa", Ui.H2, Ui.Navy);
        var lblPhamVi = Ui.Label("Phạm vi", Ui.H2, Ui.Navy);
        _cboPhamVi.Items.Add(new ComboItem("ToanTruong", "Công khai — mọi học sinh tự do vào thi"));
        _cboPhamVi.Items.Add(new ComboItem("TheoLop", "Theo lớp — chỉ học viên được phân công"));
        _cboPhamVi.SelectedIndex = 0;

        var btnTao = Ui.ButtonAccent("📅 Tạo đợt thi", 170, 42);
        Ui.GradientButton(btnTao, Ui.Accent, Ui.Accent2);
        btnTao.Click += (s, e) => Tao();
        var btnHuy = Ui.ButtonOutline("Hủy", 100, 42);
        btnHuy.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

        Controls.AddRange([lblInfo, lblTen, _txtTenDot, lblMo, _dtpMo, lblDong, _dtpDong, lblLuong, _numThoiLuong,
            lblHan, _numGioiHan, lblLan, _numLanThi, lblPhamVi, _cboPhamVi, _chkCongBo, _lblError, btnTao, btnHuy]);

        var y = 16;
        lblInfo.Location = new Point(30, y); y += 36;
        lblTen.Location = new Point(30, y); _txtTenDot.Location = new Point(30, y + 24); y += 64;
        lblMo.Location = new Point(30, y); _dtpMo.Location = new Point(30, y + 24); y += 64;
        lblDong.Location = new Point(300, y - 64); _dtpDong.Location = new Point(300, y - 40);
        lblLuong.Location = new Point(30, y); _numThoiLuong.Location = new Point(30, y + 24);
        lblHan.Location = new Point(300, y); _numGioiHan.Location = new Point(300, y + 24); y += 64;
        lblLan.Location = new Point(30, y); _numLanThi.Location = new Point(30, y + 24);
        lblPhamVi.Location = new Point(300, y); _cboPhamVi.Location = new Point(300, y + 24); y += 64;
        _chkCongBo.Location = new Point(30, y); y += 34;
        _lblError.Location = new Point(30, y);
        btnTao.Location = new Point(30, y + 22);
        btnHuy.Location = new Point(210, y + 22);
    }

    private class ComboItem(object value, string text)
    {
        public object Value { get; } = value;
        public override string ToString() => text;
    }

    private void Tao()
    {
        _lblError.Text = "";
        try
        {
            var dot = new DotThi
            {
                DeThiID = _deThiId,
                TenDotThi = _txtTenDot.Text,
                TenDe = _tenDe,
                ThoiGianMoCong = _dtpMo.Value,
                ThoiGianDongCong = _dtpDong.Value,
                ThoiLuongLamBai = (int)_numThoiLuong.Value,
                GioiHanSoLuong = _numGioiHan.Value > 0 ? (int)_numGioiHan.Value : null,
                CongBoDiemSom = _chkCongBo.Checked,
                PhamVi = (_cboPhamVi.SelectedItem as ComboItem)?.Value as string ?? "ToanTruong",
                SoLanThiToiDa = (int)_numLanThi.Value
            };
            var id = _service.TaoDotThi(dot);
            MessageBox.Show($"Tạo đợt thi thành công! (ID: {id})\nHọc viên sẽ thấy đợt thi này trên website trong khoảng thời gian mở công.",
                "Hoàn tất", MessageBoxButtons.OK, MessageBoxIcon.Information);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            _lblError.Text = "⚠ " + ex.Message;
        }
    }
}
