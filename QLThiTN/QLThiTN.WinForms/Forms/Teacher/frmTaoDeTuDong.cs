using QLThiTN.WinForms.Models;
using QLThiTN.WinForms.Services;

namespace QLThiTN.WinForms.Forms.Teacher;

/// <summary>Hop thoai tao de tu dong: chon mon, so cau, do kho -> random tu ngan hang.</summary>
public class frmTaoDeTuDong : Form
{
    private readonly QuestionService _questionService = new();
    private readonly ExamService _examService = new();
    private readonly ComboBox _cboMon = Ui.Combo(300);
    private readonly ComboBox _cboMuc = Ui.Combo(300);
    private readonly ComboBox _cboLoaiDe = Ui.Combo(300);
    private readonly NumericUpDown _numSoCau = new() { Width = 300, Minimum = 1, Maximum = 50, Value = 10, Font = Ui.Body };
    private readonly TextBox _txtTenDe = Ui.Input(300);
    private readonly Label _lblError = Ui.Label("", Ui.Small, Ui.Danger);
    private readonly Label _lblPool = Ui.Label("", Ui.Small, Ui.TextMuted);

    public frmTaoDeTuDong()
    {
        Text = "Tạo đề thi tự động";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false; MinimizeBox = false;
        ClientSize = new Size(440, 430);
        BackColor = Color.White;

        var lblMon = Ui.Label("Môn học *", Ui.H2, Ui.Navy);
        foreach (var m in _questionService.LayMonHoc()) _cboMon.Items.Add(new ComboItem(m.MonHocID, m.TenMon));
        if (_cboMon.Items.Count > 0) _cboMon.SelectedIndex = 0;
        _cboMon.SelectedIndexChanged += (s, e) => CapNhatPool();

        var lblMuc = Ui.Label("Độ khó (tùy chọn)", Ui.H2, Ui.Navy);
        _cboMuc.Items.Add(new ComboItem("", "Tất cả độ khó"));
        foreach (var (code, display) in MucDoKho.All) _cboMuc.Items.Add(new ComboItem(code, display));
        _cboMuc.SelectedIndex = 0;
        _cboMuc.SelectedIndexChanged += (s, e) => CapNhatPool();

        var lblSoCau = Ui.Label("Số lượng câu hỏi *", Ui.H2, Ui.Navy);
        var lblLoai = Ui.Label("Loại đề", Ui.H2, Ui.Navy);
        _cboLoaiDe.Items.Add(new ComboItem("ThiThu", "Thi thử"));
        _cboLoaiDe.Items.Add(new ComboItem("OnTap", "Bài tập về nhà"));
        _cboLoaiDe.Items.Add(new ComboItem("ChinhThuc", "Chính thức"));
        _cboLoaiDe.SelectedIndex = 0;

        var lblTen = Ui.Label("Tên đề thi *", Ui.H2, Ui.Navy);
        _txtTenDe.Text = $"Đề thi thử - {DateTime.Now:dd/MM/yyyy}";

        var lblGoiY = Ui.Label("Hệ thống chọn ngẫu nhiên từ ngân hàng câu hỏi\nđang hoạt động theo môn (và độ khó nếu chọn).",
            Ui.Small, Ui.TextMuted);

        var btnTao = Ui.ButtonAccent("⚡ Tạo đề", 160, 42);
        Ui.GradientButton(btnTao, Ui.Accent, Ui.Accent2);
        btnTao.Click += (s, e) => Tao();
        var btnHuy = Ui.ButtonOutline("Hủy", 100, 42);
        btnHuy.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

        Controls.AddRange([lblMon, _cboMon, lblMuc, _cboMuc, lblSoCau, _numSoCau, lblLoai, _cboLoaiDe, lblTen, _txtTenDe, _lblPool, lblGoiY, _lblError, btnTao, btnHuy]);

        var y = 18;
        lblMon.Location = new Point(30, y); _cboMon.Location = new Point(30, y + 24); y += 62;
        lblMuc.Location = new Point(30, y); _cboMuc.Location = new Point(30, y + 24); y += 62;
        lblSoCau.Location = new Point(30, y); _numSoCau.Location = new Point(30, y + 24); y += 62;
        lblLoai.Location = new Point(30, y); _cboLoaiDe.Location = new Point(30, y + 24); y += 62;
        lblTen.Location = new Point(30, y); _txtTenDe.Location = new Point(30, y + 24); y += 62;
        _lblPool.Location = new Point(30, y); y += 24;
        lblGoiY.Location = new Point(30, y); y += 44;
        _lblError.Location = new Point(30, y);
        btnTao.Location = new Point(30, y + 20);
        btnHuy.Location = new Point(200, y + 20);

        CapNhatPool();
    }

    private class ComboItem(object value, string text)
    {
        public object Value { get; } = value;
        public override string ToString() => text;
    }

    private void CapNhatPool()
    {
        if (_cboMon.SelectedItem == null) return;
        var mon = (int)(_cboMon.SelectedItem as ComboItem)!.Value!;
        var muc = (_cboMuc.SelectedItem as ComboItem)?.Value as string ?? "";
        var dt = _examService.LayCauHoiPool(mon, muc);
        _lblPool.Text = $"Ngân hàng có {dt.Rows.Count} câu phù hợp.";
    }

    private void Tao()
    {
        _lblError.Text = "";
        var mon = (int)(_cboMon.SelectedItem as ComboItem)!.Value!;
        var muc = (_cboMuc.SelectedItem as ComboItem)?.Value as string ?? "";
        var loai = (_cboLoaiDe.SelectedItem as ComboItem)?.Value as string ?? "ThiThu";

        var (id, error) = _examService.TaoDeTuDong(_txtTenDe.Text, mon, (int)_numSoCau.Value, muc, loai);
        if (id == 0) { _lblError.Text = "⚠ " + error; return; }
        MessageBox.Show($"Tạo đề thành công! (ID: {id})\nHãy chọn đề và tạo 'Đợt thi' để học viên vào làm.",
            "Hoàn tất", MessageBoxButtons.OK, MessageBoxIcon.Information);
        DialogResult = DialogResult.OK;
        Close();
    }
}
