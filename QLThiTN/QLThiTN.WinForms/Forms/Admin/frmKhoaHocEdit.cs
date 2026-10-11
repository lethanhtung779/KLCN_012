using QLThiTN.WinForms.Models;
using QLThiTN.WinForms.Services;

namespace QLThiTN.WinForms.Forms.Admin;

/// <summary>Them / sua khoa hoc.</summary>
public class frmKhoaHocEdit : Form
{
    private readonly TrainingService _service = new();
    private readonly int _id;
    private readonly TextBox _txtTen = Ui.Input(320);
    private readonly ComboBox _cboMon = Ui.Combo(320);
    private readonly DateTimePicker _dtBatDau = new() { Format = DateTimePickerFormat.Short, ShowCheckBox = true, Checked = false };
    private readonly DateTimePicker _dtKetThuc = new() { Format = DateTimePickerFormat.Short, ShowCheckBox = true, Checked = false };
    private readonly ComboBox _cboTrangThai = Ui.Combo(320);
    private readonly Label _lblError = Ui.Label("", Ui.Small, Ui.Danger);

    public frmKhoaHocEdit(
        int id = 0,
        string tenKhoaHoc = "",
        int monHocId = 0,
        DateTime? ngayBatDau = null,
        DateTime? ngayKetThuc = null,
        string trangThai = TrangThaiKhoaHoc.HoatDong)
    {
        _id = id;
        Text = id == 0 ? "Mở khóa học mới" : "Sửa khóa học";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(400, 380);
        BackColor = Color.White;

        NapMonHoc(monHocId);
        foreach (var (_, display) in TrangThaiKhoaHoc.All)
            _cboTrangThai.Items.Add(display);
        _cboTrangThai.SelectedItem = TrangThaiKhoaHoc.Display(trangThai);

        _txtTen.Text = tenKhoaHoc;
        if (ngayBatDau.HasValue) { _dtBatDau.Checked = true; _dtBatDau.Value = ngayBatDau.Value; }
        if (ngayKetThuc.HasValue) { _dtKetThuc.Checked = true; _dtKetThuc.Value = ngayKetThuc.Value; }

        var lblTen = Ui.Label("Tên khóa học *", Ui.H2, Ui.Navy);
        var lblMon = Ui.Label("Môn học *", Ui.H2, Ui.Navy);
        var lblBd = Ui.Label("Ngày bắt đầu", Ui.H2, Ui.Navy);
        var lblKt = Ui.Label("Ngày kết thúc", Ui.H2, Ui.Navy);
        var lblTt = Ui.Label("Trạng thái", Ui.H2, Ui.Navy);

        var btnLuu = Ui.ButtonAccent("Lưu", 120, 40);
        Ui.GradientButton(btnLuu, Ui.Accent, Ui.Accent2);
        btnLuu.Click += (_, _) => Luu();
        var btnHuy = Ui.ButtonOutline("Hủy", 100, 40);
        btnHuy.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

        Controls.AddRange([
            lblTen, _txtTen, lblMon, _cboMon, lblBd, _dtBatDau, lblKt, _dtKetThuc, lblTt, _cboTrangThai,
            _lblError, btnLuu, btnHuy
        ]);

        var y = 20;
        lblTen.Location = new Point(28, y); _txtTen.Location = new Point(28, y + 24); y += 58;
        lblMon.Location = new Point(28, y); _cboMon.Location = new Point(28, y + 24); y += 58;
        lblBd.Location = new Point(28, y); _dtBatDau.Location = new Point(28, y + 24); _dtBatDau.Width = 320; y += 58;
        lblKt.Location = new Point(28, y); _dtKetThuc.Location = new Point(28, y + 24); _dtKetThuc.Width = 320; y += 58;
        lblTt.Location = new Point(28, y); _cboTrangThai.Location = new Point(28, y + 24); y += 58;
        _lblError.Location = new Point(28, y);
        btnLuu.Location = new Point(28, y + 24);
        btnHuy.Location = new Point(158, y + 24);
    }

    private void NapMonHoc(int selectedId)
    {
        _cboMon.DisplayMember = "TenMon";
        _cboMon.ValueMember = "MonHocID";
        _cboMon.DataSource = _service.LayDanhSachMonHoc();
        if (selectedId > 0)
        {
            _cboMon.SelectedValue = selectedId;
            if (_cboMon.SelectedIndex < 0 && _cboMon.Items.Count > 0)
                _cboMon.SelectedIndex = 0;
        }
    }

    private string MaTrangThai() =>
        TrangThaiKhoaHoc.All.FirstOrDefault(x => x.Display == _cboTrangThai.Text).Code
        ?? TrangThaiKhoaHoc.HoatDong;

    private void Luu()
    {
        _lblError.Text = "";
        var monId = _cboMon.SelectedValue is int m ? m : 0;
        DateTime? bd = _dtBatDau.Checked ? _dtBatDau.Value.Date : null;
        DateTime? kt = _dtKetThuc.Checked ? _dtKetThuc.Value.Date : null;
        if (bd.HasValue && kt.HasValue && kt < bd)
        {
            _lblError.Text = "⚠ Ngày kết thúc phải sau ngày bắt đầu.";
            return;
        }

        var tt = MaTrangThai();
        if (_id == 0)
        {
            var (ok, error, _) = _service.ThemKhoaHoc(_txtTen.Text, monId, bd, kt, tt);
            if (!ok) { _lblError.Text = "⚠ " + error; return; }
        }
        else
        {
            var (ok, error) = _service.SuaKhoaHoc(_id, _txtTen.Text, monId, bd, kt, tt);
            if (!ok) { _lblError.Text = "⚠ " + error; return; }
        }
        DialogResult = DialogResult.OK;
        Close();
    }
}
