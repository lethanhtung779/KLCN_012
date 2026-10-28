using System.Data;
using QLThiTN.WinForms.Services;

namespace QLThiTN.WinForms.Forms.Admin;

/// <summary>Gan / sua giao vien day mon cho lop.</summary>
public class frmPhanCongEdit : Form
{
    private readonly TrainingService _service = new();
    private readonly int _lopHocId;
    private readonly bool _sua;
    private int _monHocIdCoDinh;
    private readonly int _giaoVienIdBanDau;
    private readonly Label _lblMonHoc = Ui.Label("", Ui.Body, Ui.Navy);
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

        var lblMon = Ui.Label("Môn học theo khóa học", Ui.H2, Ui.Navy);
        var lblGv = Ui.Label("Giáo viên *", Ui.H2, Ui.Navy);
        var btnLuu = Ui.ButtonAccent("Lưu", 120, 40);
        Ui.GradientButton(btnLuu, Ui.Accent, Ui.Accent2);
        btnLuu.Click += (_, _) => Luu();
        var btnHuy = Ui.ButtonOutline("Hủy", 100, 40);
        btnHuy.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

        Controls.AddRange([lblMon, _lblMonHoc, lblGv, _cboGiaoVien, _lblError, btnLuu, btnHuy]);
        lblMon.Location = new Point(28, 20); _lblMonHoc.Location = new Point(28, 48);
        _lblMonHoc.AutoSize = false;
        _lblMonHoc.Size = new Size(460, 28);
        _lblMonHoc.AutoEllipsis = true;
        lblGv.Location = new Point(28, 82); _cboGiaoVien.Location = new Point(28, 106);
        _lblError.Location = new Point(28, 144);
        btnLuu.Location = new Point(28, 172);
        btnHuy.Location = new Point(158, 172);

        Load += (_, _) =>
        {
            try
            {
                var mon = _service.LayMonHocCuaLop(_lopHocId);
                if (mon.Rows.Count == 0)
                    throw new InvalidOperationException("Không tìm thấy môn học của lớp.");
                var monId = Convert.ToInt32(mon.Rows[0]["MonHocID"]);
                if (_sua && monHocId != monId)
                    throw new InvalidOperationException("Phân công cũ không khớp môn của khóa học. Hãy xóa và phân công lại.");
                _monHocIdCoDinh = monId;
                _lblMonHoc.Text = mon.Rows[0]["TenMon"].ToString();
                NapGiaoVienTheoMon(_giaoVienIdBanDau);
            }
            catch (Exception ex)
            {
                _lblError.Text = ex.Message;
                btnLuu.Enabled = false;
            }
        };
    }

    private int LayMonIdDangChon() => _monHocIdCoDinh;

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
            _lblError.Text = "⚠ Hãy chọn giáo viên phù hợp với môn của lớp.";
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
