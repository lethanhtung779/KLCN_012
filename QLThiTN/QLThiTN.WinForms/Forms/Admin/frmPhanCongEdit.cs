using QLThiTN.WinForms.Services;

namespace QLThiTN.WinForms.Forms.Admin;

/// <summary>Gan / sua giao vien day mon cho lop.</summary>
public class frmPhanCongEdit : Form
{
    private readonly TrainingService _service = new();
    private readonly int _lopHocId;
    private readonly bool _sua;
    private readonly int _monHocIdCoDinh;
    private readonly ComboBox _cboMon = Ui.Combo(300);
    private readonly ComboBox _cboGiaoVien = Ui.Combo(300);
    private readonly Label _lblError = Ui.Label("", Ui.Small, Ui.Danger);

    public frmPhanCongEdit(int lopHocId, bool sua = false, int monHocId = 0, int giaoVienId = 0)
    {
        _lopHocId = lopHocId;
        _sua = sua;
        _monHocIdCoDinh = monHocId;

        Text = sua ? "Sửa phân công giảng dạy" : "Phân công giảng dạy";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(380, 240);
        BackColor = Color.White;

        if (sua)
        {
            NapMonCoDinh(monHocId);
            _cboMon.Enabled = false;
        }
        else
        {
            _cboMon.DisplayMember = "TenMon";
            _cboMon.ValueMember = "MonHocID";
            _cboMon.DataSource = _service.LayMonHocChuaPhanCong(lopHocId);
            _cboMon.SelectedIndexChanged += (_, _) => NapGiaoVienTheoMon();
        }

        NapGiaoVienTheoMon(giaoVienId);

        var lblMon = Ui.Label("Môn học *", Ui.H2, Ui.Navy);
        var lblGv = Ui.Label("Giáo viên *", Ui.H2, Ui.Navy);
        var btnLuu = Ui.ButtonAccent("Lưu", 120, 40);
        Ui.GradientButton(btnLuu, Ui.Accent, Ui.Accent2);
        btnLuu.Click += (_, _) => Luu();
        var btnHuy = Ui.ButtonOutline("Hủy", 100, 40);
        btnHuy.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

        Controls.AddRange([lblMon, _cboMon, lblGv, _cboGiaoVien, _lblError, btnLuu, btnHuy]);
        lblMon.Location = new Point(28, 20); _cboMon.Location = new Point(28, 44);
        lblGv.Location = new Point(28, 78); _cboGiaoVien.Location = new Point(28, 102);
        _lblError.Location = new Point(28, 138);
        btnLuu.Location = new Point(28, 158);
        btnHuy.Location = new Point(158, 158);
    }

    private void NapMonCoDinh(int monHocId)
    {
        var dt = _service.LayDanhSachMonHoc();
        _cboMon.DisplayMember = "TenMon";
        _cboMon.ValueMember = "MonHocID";
        _cboMon.DataSource = dt;
        if (monHocId > 0) _cboMon.SelectedValue = monHocId;
    }

    private void NapGiaoVienTheoMon(int? chonGv = null)
    {
        var monId = _sua ? _monHocIdCoDinh : _cboMon.SelectedValue is int m ? m : 0;
        if (monId <= 0)
        {
            _cboGiaoVien.DataSource = null;
            return;
        }
        var gv = _service.LayGiaoVienTheoMon(monId);
        if (gv.Rows.Count == 0)
            gv = _service.LayTatCaGiaoVien();
        _cboGiaoVien.DisplayMember = "HoTen";
        _cboGiaoVien.ValueMember = "GiaoVienID";
        _cboGiaoVien.DataSource = gv;
        if (chonGv.HasValue && chonGv.Value > 0)
            _cboGiaoVien.SelectedValue = chonGv.Value;
    }

    private void Luu()
    {
        _lblError.Text = "";
        var monId = _sua ? _monHocIdCoDinh : _cboMon.SelectedValue is int m ? m : 0;
        var gvId = _cboGiaoVien.SelectedValue is int g ? g : 0;
        var (ok, error) = _service.LuuPhanCong(_lopHocId, monId, gvId, _sua);
        if (!ok) { _lblError.Text = "⚠ " + error; return; }
        DialogResult = DialogResult.OK;
        Close();
    }
}
