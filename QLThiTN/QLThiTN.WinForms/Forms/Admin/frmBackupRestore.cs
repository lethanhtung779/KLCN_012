using QLThiTN.WinForms.Services;

namespace QLThiTN.WinForms.Forms.Admin;

/// <summary>Sao luu / phuc hoi co so du lieu ThiTracNghiem.</summary>
public class frmBackupRestore : Form
{
    private readonly BackupService _service = new();
    private readonly TextBox _txtThuMuc = Ui.Input(360);
    private readonly TextBox _txtFileRestore = Ui.Input(360);
    private readonly ListBox _lstFile = new();
    private readonly Label _lblStatus = Ui.Label("Sẵn sàng.", Ui.Body, Ui.TextMuted);

    public frmBackupRestore()
    {
        Text = "Sao lưu & phục hồi dữ liệu";
        Font = Ui.Body;
        ClientSize = new Size(860, 520);
        BackColor = Ui.BgSoft;

        // Panel sao luu
        var grpBackup = new GroupBox
        {
            Text = "1. Sao lưu (Backup)",
            Font = Ui.H2,
            ForeColor = Ui.Navy,
            Location = new Point(20, 16),
            Size = new Size(820, 150),
            BackColor = Color.White
        };
        var lblMuc = Ui.Label("Thư mục lưu file backup:", Ui.Body);
        _txtThuMuc.Text = BackupService.ThuMucBackupMacDinh;
        var btnChonMuc = Ui.ButtonOutline("Chọn...", 90);
        btnChonMuc.Click += (s, e) =>
        {
            using var dlg = new FolderBrowserDialog();
            if (Directory.Exists(_txtThuMuc.Text)) dlg.SelectedPath = _txtThuMuc.Text;
            if (dlg.ShowDialog(this) == DialogResult.OK) _txtThuMuc.Text = dlg.SelectedPath;
        };
        var btnBackup = Ui.ButtonAccent("🗄 Sao lưu ngay", 180, 40);
        Ui.GradientButton(btnBackup, Ui.Accent, Ui.Accent2);
        btnBackup.Click += (s, e) => SaoLuu();

        grpBackup.Controls.AddRange([lblMuc, _txtThuMuc, btnChonMuc, btnBackup]);
        lblMuc.Location = new Point(20, 36);
        _txtThuMuc.Location = new Point(20, 60);
        btnChonMuc.Location = new Point(390, 58);
        btnBackup.Location = new Point(20, 95);

        // Panel phuc hoi
        var grpRestore = new GroupBox
        {
            Text = "2. Phục hồi (Restore) — cẩn thận: dữ liệu hiện tại sẽ bị thay thế",
            Font = Ui.H2,
            ForeColor = Ui.Navy,
            Location = new Point(20, 180),
            Size = new Size(820, 300),
            BackColor = Color.White
        };
        var lblGoiY = Ui.Label("Chọn file .bak trong danh sách (thư mục mặc định) hoặc bấm Duyệt file:", Ui.Body);
        _lstFile.Location = new Point(20, 64);
        _lstFile.Size = new Size(500, 180);
        _lstFile.Font = Ui.Body;
        var btnDuyet = Ui.ButtonOutline("Duyệt file...", 120);
        btnDuyet.Click += (s, e) =>
        {
            using var dlg = new OpenFileDialog { Filter = "Backup file (*.bak)|*.bak", Title = "Chọn file backup" };
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                _txtFileRestore.Text = dlg.FileName;
                _lstFile.ClearSelected();
            }
        };
        var btnTaiLai = Ui.ButtonOutline("Tải lại danh sách", 150);
        btnTaiLai.Click += (s, e) => TaiDanhSachFile();
        var btnRestore = Ui.ButtonDanger("⚠ Phục hồi ngay", 180, 40);
        btnRestore.Click += (s, e) => PhucHoi();

        grpRestore.Controls.AddRange([lblGoiY, _lstFile, _txtFileRestore, btnDuyet, btnTaiLai, btnRestore]);
        lblGoiY.Location = new Point(20, 34);
        btnDuyet.Location = new Point(540, 64);
        btnTaiLai.Location = new Point(540, 110);
        _txtFileRestore.Location = new Point(20, 250);
        btnRestore.Location = new Point(20, 255);
        btnRestore.Location = new Point(540, 250);

        _lblStatus.Location = new Point(20, 490);

        Controls.AddRange([grpBackup, grpRestore, _lblStatus]);
        Load += (s, e) => TaiDanhSachFile();
    }

    private void TaiDanhSachFile()
    {
        _lstFile.Items.Clear();
        var muc = _txtThuMuc.Text;
        if (!Directory.Exists(muc)) return;
        foreach (var f in Directory.GetFiles(muc, "*.bak").OrderByDescending(f => f))
            _lstFile.Items.Add(Path.GetFileName(f));
    }

    private void SaoLuu()
    {
        try
        {
            SetStatus("Đang sao lưu...");
            Application.DoEvents();
            var file = _service.SaoLuu(_txtThuMuc.Text);
            SetStatus($"✔ Sao lưu thành công: {file}");
            MessageBox.Show(this, $"Sao lưu thành công!\n{file}", "Hoàn tất", MessageBoxButtons.OK, MessageBoxIcon.Information);
            TaiDanhSachFile();
        }
        catch (Exception ex)
        {
            SetStatus("✖ Sao lưu thất bại: " + ex.Message);
            MessageBox.Show(this, "Sao lưu thất bại:\n" + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void PhucHoi()
    {
        var file = _lstFile.SelectedItem != null
            ? Path.Combine(_txtThuMuc.Text, _lstFile.SelectedItem.ToString()!)
            : _txtFileRestore.Text;

        if (string.IsNullOrWhiteSpace(file) || !File.Exists(file))
        {
            MessageBox.Show(this, "Hãy chọn một file backup hợp lệ.", "Thông báo");
            return;
        }

        if (MessageBox.Show(this,
                "PHỤC HỒI sẽ thay thế toàn bộ dữ liệu hiện tại bằng dữ liệu trong file backup.\nWebsite đang chạy cũng sẽ bị ngắt kết nối trong quá trình restore.\n\nTiếp tục?",
                "Cảnh báo phục hồi", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

        try
        {
            SetStatus("Đang phục hồi... Vui lòng chờ, website sẽ tạm ngắt kết nối.");
            Application.DoEvents();
            _service.PhucHoi(file);
            SetStatus("✔ Phục hồi thành công từ: " + file);
            MessageBox.Show(this, "Phục hồi dữ liệu thành công!", "Hoàn tất", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            SetStatus("✖ Phục hồi thất bại: " + ex.Message);
            MessageBox.Show(this, "Phục hồi thất bại:\n" + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void SetStatus(string text) => _lblStatus.Text = text;
}
