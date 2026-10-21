using System.Data;
using QLThiTN.WinForms.Services;

namespace QLThiTN.WinForms.Forms.Admin;

/// <summary>Gan / sua giao vien day mon cho lop.</summary>
public class frmPhanCongEdit : Form
{
    private readonly TrainingService _service = new();
    private readonly int _lopHocId;
    private readonly bool _sua;
    private readonly int _monHocIdCoDinh;
    private readonly int _giaoVienIdBanDau;
    private readonly ComboBox _cboMon = Ui.Combo(460);
    private readonly ComboBox _cboGiaoVien = Ui.Combo(460);
    private readonly Label _lblError = Ui.Label("", Ui.Small, Ui.Danger);

    public frmPhanCongEdit(int lopHocId, bool sua = false, int monHocId = 0, int giaoVienId = 0)
    {
        _lopHocId = lopHocId;
        _sua = sua;
        _monHocIdCoDinh = monHocId;
        _giaoVienIdBanDau = giaoVienId;

        Text = sua ? "Sửa phân công giảng dạy" : "Phân công giảng dạy";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(520, 260);
        BackColor = Color.White;

        var lblMon = Ui.Label("Môn học *", Ui.H2, Ui.Navy);
        var lblGv = Ui.Label("Giáo viên *", Ui.H2, Ui.Navy);
        var btnLuu = Ui.ButtonAccent("Lưu", 120, 40);
        Ui.GradientButton(btnLuu, Ui.Accent, Ui.Accent2);
        btnLuu.Click += (_, _) => Luu();
        var btnHuy = Ui.ButtonOutline("Hủy", 100, 40);
        btnHuy.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

        Controls.AddRange([lblMon, _cboMon, lblGv, _cboGiaoVien, _lblError, btnLuu, btnHuy]);
        lblMon.Location = new Point(28, 20); _cboMon.Location = new Point(28, 44);
        lblGv.Location = new Point(28, 82); _cboGiaoVien.Location = new Point(28, 106);
        _lblError.Location = new Point(28, 144);
        btnLuu.Location = new Point(28, 172);
        btnHuy.Location = new Point(158, 172);

        _cboMon.SelectedIndexChanged += (_, _) => NapGiaoVienTheoMon();

        Load += (_, _) =>
        {
            if (_sua)
            {
                NapMonCoDinh(_monHocIdCoDinh);
                _cboMon.Enabled = false;
            }
            else
            {
                _cboMon.DisplayMember = "TenMon";
                _cboMon.ValueMember = "MonHocID";
                _cboMon.DataSource = _service.LayMonHocChuaPhanCong(_lopHocId);
            }
            NapGiaoVienTheoMon(_giaoVienIdBanDau);
        };
    }

    private void NapMonCoDinh(int monHocId)
    {
        var dt = _service.LayDanhSachMonHoc();
        _cboMon.DisplayMember = "TenMon";
        _cboMon.ValueMember = "MonHocID";
        _cboMon.DataSource = dt;
        if (monHocId > 0) _cboMon.SelectedValue = monHocId;
    }

    private int LayMonIdDangChon()
    {
        if (_sua) return _monHocIdCoDinh;
        if (_cboMon.SelectedValue is int m && m > 0) return m;
        if (_cboMon.SelectedItem is DataRowView drv && drv["MonHocID"] != DBNull.Value)
            return Convert.ToInt32(drv["MonHocID"]);
        if (int.TryParse(_cboMon.SelectedValue?.ToString(), out var parsed) && parsed > 0)
            return parsed;
        return 0;
    }

    private void NapGiaoVienTheoMon(int? chonGv = null)
    {
        var monId = LayMonIdDangChon();
        if (monId <= 0)
        {
            _cboGiaoVien.DataSource = null;
            return;
        }
        var gv = _service.LayGiaoVienTheoMon(monId);
        _cboGiaoVien.DisplayMember = "HoTen";
        _cboGiaoVien.ValueMember = "GiaoVienID";
        _cboGiaoVien.DataSource = gv;
        _lblError.Text = gv.Rows.Count == 0
            ? "Chưa có giáo viên đúng chuyên môn. Hãy cập nhật môn trong hồ sơ giáo viên."
            : "";
        _cboGiaoVien.DropDownWidth = Math.Max(_cboGiaoVien.Width, 460);
        if (chonGv.HasValue && chonGv.Value > 0)
            _cboGiaoVien.SelectedValue = chonGv.Value;
        else if (_cboGiaoVien.Items.Count > 0)
            _cboGiaoVien.SelectedIndex = 0;
    }

    private void Luu()
    {
        _lblError.Text = "";
        var monId = LayMonIdDangChon();
        var gvId = GetSelectedId(_cboGiaoVien, "GiaoVienID");
        if (monId <= 0 || gvId <= 0)
        {
            _lblError.Text = "⚠ Hãy chọn đủ môn học và giáo viên.";
            return;
        }
        var (ok, error) = _service.LuuPhanCong(_lopHocId, monId, gvId, _sua);
        if (!ok) { _lblError.Text = "⚠ " + error; return; }
        DialogResult = DialogResult.OK;
        Close();
    }

    private static int GetSelectedId(ComboBox cbo, string valueMember)
    {
        if (cbo.SelectedValue is int i && i > 0) return i;
        if (cbo.SelectedItem is DataRowView drv && drv[valueMember] != DBNull.Value)
            return Convert.ToInt32(drv[valueMember]);
        if (int.TryParse(cbo.SelectedValue?.ToString(), out var p) && p > 0) return p;
        return 0;
    }
}
