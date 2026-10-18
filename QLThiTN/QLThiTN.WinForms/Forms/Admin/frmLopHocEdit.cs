using QLThiTN.WinForms.Services;

namespace QLThiTN.WinForms.Forms.Admin;

/// <summary>Them / sua lop hoc.</summary>
public class frmLopHocEdit : Form
{
    private readonly TrainingService _service = new();
    private readonly int _id;
    private readonly TextBox _txtTen = Ui.Input(320);
    private readonly ComboBox _cboKhoa = Ui.Combo(320);
    private readonly Label _lblError = Ui.Label("", Ui.Small, Ui.Danger);

    public frmLopHocEdit(int id = 0, string tenLop = "", int khoaHocId = 0)
    {
        _id = id;
        Text = id == 0 ? "Mở lớp học mới" : "Sửa lớp học";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(400, 220);
        BackColor = Color.White;

        _txtTen.Text = tenLop;
        NapKhoaHoc(khoaHocId);

        var lblTen = Ui.Label("Tên lớp *", Ui.H2, Ui.Navy);
        var lblKhoa = Ui.Label("Khóa học *", Ui.H2, Ui.Navy);
        var btnLuu = Ui.ButtonAccent("Lưu", 120, 40);
        Ui.GradientButton(btnLuu, Ui.Accent, Ui.Accent2);
        btnLuu.Click += (_, _) => Luu();
        var btnHuy = Ui.ButtonOutline("Hủy", 100, 40);
        btnHuy.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

        Controls.AddRange([lblTen, _txtTen, lblKhoa, _cboKhoa, _lblError, btnLuu, btnHuy]);
        lblTen.Location = new Point(28, 20); _txtTen.Location = new Point(28, 44);
        lblKhoa.Location = new Point(28, 78); _cboKhoa.Location = new Point(28, 102);
        _lblError.Location = new Point(28, 138);
        btnLuu.Location = new Point(28, 158);
        btnHuy.Location = new Point(158, 158);
    }

    private void NapKhoaHoc(int selectedId)
    {
        _cboKhoa.DisplayMember = "TenHienThi";
        _cboKhoa.ValueMember = "KhoaHocID";
        var khoaHoc = _service.LayComboKhoaHoc();
        if (khoaHoc.Rows.Count == 0)
        {
            MessageBox.Show("Chưa có khóa học nào. Hãy tạo khóa học trước khi mở lớp.", "Thông báo",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        _cboKhoa.DataSource = khoaHoc;
        if (selectedId > 0) _cboKhoa.SelectedValue = selectedId;
    }

    private void Luu()
    {
        _lblError.Text = "";
        if (_cboKhoa.SelectedValue is not int khoaId || khoaId <= 0)
        {
            _lblError.Text = "⚠ Hãy chọn khóa học.";
            return;
        }
        if (_id == 0)
        {
            var (ok, error, _) = _service.ThemLopHoc(_txtTen.Text, khoaId);
            if (!ok) { _lblError.Text = "⚠ " + error; return; }
        }
        else
        {
            var (ok, error) = _service.SuaLopHoc(_id, _txtTen.Text, khoaId);
            if (!ok) { _lblError.Text = "⚠ " + error; return; }
        }
        DialogResult = DialogResult.OK;
        Close();
    }
}
