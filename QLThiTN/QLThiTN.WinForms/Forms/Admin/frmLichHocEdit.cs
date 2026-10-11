using QLThiTN.WinForms.Services;

namespace QLThiTN.WinForms.Forms.Admin;

/// <summary>Them / sua buoi hoc trong LichHoc.</summary>
public class frmLichHocEdit : Form
{
    private readonly TrainingService _service = new();
    private readonly int _id;
    private readonly ComboBox _cboLop = Ui.Combo(320);
    private readonly TextBox _txtNoiDung = Ui.Input(320);
    private readonly DateTimePicker _dtBatDau = new() { Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy HH:mm", Width = 320 };
    private readonly DateTimePicker _dtKetThuc = new() { Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy HH:mm", Width = 320 };
    private readonly TextBox _txtDiaDiem = Ui.Input(320);
    private readonly Label _lblError = Ui.Label("", Ui.Small, Ui.Danger);

    public frmLichHocEdit(
        int id = 0,
        int lopHocId = 0,
        string? noiDung = null,
        DateTime? batDau = null,
        DateTime? ketThuc = null,
        string? diaDiem = null)
    {
        _id = id;
        Text = id == 0 ? "Thêm buổi học" : "Sửa buổi học";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(400, 360);
        BackColor = Color.White;

        NapLop(lopHocId);
        _txtNoiDung.Text = noiDung ?? "";
        _dtBatDau.Value = batDau ?? DateTime.Now.Date.AddHours(8);
        _dtKetThuc.Value = ketThuc ?? DateTime.Now.Date.AddHours(10);
        _txtDiaDiem.Text = diaDiem ?? "";

        var lblLop = Ui.Label("Lớp học *", Ui.H2, Ui.Navy);
        var lblNd = Ui.Label("Nội dung buổi học", Ui.H2, Ui.Navy);
        var lblBd = Ui.Label("Bắt đầu *", Ui.H2, Ui.Navy);
        var lblKt = Ui.Label("Kết thúc *", Ui.H2, Ui.Navy);
        var lblDd = Ui.Label("Địa điểm / phòng", Ui.H2, Ui.Navy);

        var btnLuu = Ui.ButtonAccent("Lưu", 120, 40);
        Ui.GradientButton(btnLuu, Ui.Accent, Ui.Accent2);
        btnLuu.Click += (_, _) => Luu();
        var btnHuy = Ui.ButtonOutline("Hủy", 100, 40);
        btnHuy.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

        Controls.AddRange([
            lblLop, _cboLop, lblNd, _txtNoiDung, lblBd, _dtBatDau, lblKt, _dtKetThuc, lblDd, _txtDiaDiem,
            _lblError, btnLuu, btnHuy
        ]);

        var y = 16;
        lblLop.Location = new Point(28, y); _cboLop.Location = new Point(28, y + 22); y += 52;
        lblNd.Location = new Point(28, y); _txtNoiDung.Location = new Point(28, y + 22); y += 52;
        lblBd.Location = new Point(28, y); _dtBatDau.Location = new Point(28, y + 22); y += 52;
        lblKt.Location = new Point(28, y); _dtKetThuc.Location = new Point(28, y + 22); y += 52;
        lblDd.Location = new Point(28, y); _txtDiaDiem.Location = new Point(28, y + 22); y += 52;
        _lblError.Location = new Point(28, y);
        btnLuu.Location = new Point(28, y + 20);
        btnHuy.Location = new Point(158, y + 20);
    }

    private void NapLop(int selectedId)
    {
        _cboLop.DisplayMember = "TenHienThi";
        _cboLop.ValueMember = "LopHocID";
        _cboLop.DataSource = _service.LayComboLopHoc();
        if (_cboLop.Items.Count == 0)
        {
            MessageBox.Show("Chưa có lớp học. Hãy mở lớp trước.", "Thông báo");
            return;
        }
        if (selectedId > 0) _cboLop.SelectedValue = selectedId;
    }

    private void Luu()
    {
        _lblError.Text = "";
        if (_cboLop.SelectedValue is not int lopId || lopId <= 0)
        {
            _lblError.Text = "⚠ Hãy chọn lớp.";
            return;
        }
        if (_id == 0)
        {
            var (ok, error, _) = _service.ThemLichHoc(lopId, _txtNoiDung.Text, _dtBatDau.Value, _dtKetThuc.Value, _txtDiaDiem.Text);
            if (!ok) { _lblError.Text = "⚠ " + error; return; }
        }
        else
        {
            var (ok, error) = _service.SuaLichHoc(_id, lopId, _txtNoiDung.Text, _dtBatDau.Value, _dtKetThuc.Value, _txtDiaDiem.Text);
            if (!ok) { _lblError.Text = "⚠ " + error; return; }
        }
        DialogResult = DialogResult.OK;
        Close();
    }
}
