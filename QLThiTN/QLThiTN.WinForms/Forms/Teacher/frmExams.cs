using System.Data;
using QLThiTN.WinForms.Services;

namespace QLThiTN.WinForms.Forms.Teacher;

/// <summary>Quan ly de thi & ky thi: danh sach de, tao de tu dong/thu cong, tao dot thi.</summary>
public class frmExams : Form
{
    private readonly ExamService _service = new();
    private readonly DataGridView _gridDe = new();
    private readonly DataGridView _gridDot = new();
    private readonly SplitContainer _split = new();

    public frmExams()
    {
        ExamService.YeuCauQuanTriVien();
        Text = "Đề thi & Kỳ thi";
        Font = Ui.Body;

        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(16, 10, 0, 0),
            BackColor = Color.White,
            WrapContents = true
        };
        var btnTHPTQG = Ui.ButtonAccent("🎓 Tạo đề chuẩn THPTQG", 220);
        btnTHPTQG.Click += (s, e) => TaoDe(giaoVienId => new frmTaoDeTHPTQG(giaoVienId));
        var btnTuDong = Ui.ButtonPrimary("⚡ Tạo đề tự động", 180);
        btnTuDong.Click += (s, e) => TaoDe(giaoVienId => new frmTaoDeTuDong(giaoVienId));
        var btnThuCong = Ui.ButtonPrimary("✎ Tạo đề thủ công", 190);
        btnThuCong.Click += (s, e) => TaoDe(giaoVienId => new frmTaoDeThuCong(giaoVienId));
        var btnDotThi = Ui.ButtonPrimary("📅 Tạo đợt thi", 160);
        btnDotThi.Click += (s, e) => TaoDotThi();
        var btnTronDe = Ui.ButtonPrimary("🔀 Trộn mã đề", 160);
        btnTronDe.Click += (s, e) => MoManHinhTronDe();
        var btnXoaDe = Ui.ButtonDanger("🗑 Xóa đề", 110);
        btnXoaDe.Click += (s, e) => XoaDe();

        foreach (var c in new Control[] { btnTHPTQG, btnTuDong, btnThuCong, btnDotThi, btnTronDe, btnXoaDe })
        {
            c.Margin = new Padding(0, 3, 10, 0);
            toolbar.Controls.Add(c);
        }

        _split.Dock = DockStyle.Fill;
        _split.Orientation = Orientation.Horizontal;
        _split.SplitterDistance = 300;

        // Grid de thi (tren)
        var lblDe = Ui.Label("Danh sách đề thi", Ui.H2, Ui.Navy);
        var panelDe = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(0, 4, 0, 0) };
        Ui.StyleGrid(_gridDe);
        _gridDe.Dock = DockStyle.Fill;
        _gridDe.SelectionChanged += (s, e) => TaiDotThi();
        panelDe.Controls.Add(_gridDe);
        panelDe.Controls.Add(lblDe);
        lblDe.Location = new Point(16, 8);
        _gridDe.Location = new Point(0, 36);

        var panelDeWrap = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(0, 0, 0, 0) };
        panelDeWrap.Controls.Add(panelDe);
        _split.Panel1.Controls.Add(panelDeWrap);

        // Grid dot thi (duoi)
        var lblDot = Ui.Label("Các đợt thi của đề đang chọn (chọn đề phía trên)", Ui.H2, Ui.Navy);
        var panelDot = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
        Ui.StyleGrid(_gridDot);
        _gridDot.Dock = DockStyle.Fill;
        var btnDongDot = Ui.ButtonOutline("Đóng đợt thi", 140, 34);
        btnDongDot.Location = new Point(600, 8);
        btnDongDot.Click += (s, e) => DongDot();
        panelDot.Controls.Add(_gridDot);
        panelDot.Controls.Add(lblDot);
        panelDot.Controls.Add(btnDongDot);
        lblDot.Location = new Point(16, 10);
        _gridDot.Location = new Point(0, 40);
        _split.Panel2.Controls.Add(panelDot);
        _split.Panel2.BackColor = Color.White;

        Controls.Add(_split);
        Controls.Add(toolbar);

        Load += (s, e) => { try { _service.DongDotThiQuaHan(); } catch { } TaiLai(); };
    }

    private void TaoDe(Func<int, Form> createForm)
    {
        using var chooser = new Form
        {
            Text = "Giáo viên phụ trách đề thi", ClientSize = new Size(510, 180),
            StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false, MinimizeBox = false, Font = Ui.Body
        };
        var teachers = Database.Query(@"SELECT gv.GiaoVienID, gv.HoTen + N' (' + tk.TenDangNhap + N')' AS Ten
            FROM GiaoVien gv JOIN TaiKhoan tk ON tk.TaiKhoanID = gv.TaiKhoanID
            WHERE tk.TrangThai = N'HoatDong' AND tk.VaiTro = N'GiaoVien' ORDER BY gv.HoTen");
        if (teachers.Rows.Count == 0) { MessageBox.Show("Chưa có giáo viên đang hoạt động để phụ trách đề thi."); return; }
        var label = Ui.Label("Chọn giáo viên phụ trách đề thi:", Ui.Body, Ui.Navy);
        label.Location = new Point(20, 20);
        var combo = Ui.Combo(460); combo.Location = new Point(20, 50);
        combo.DisplayMember = "Ten"; combo.ValueMember = "GiaoVienID"; combo.DataSource = teachers; combo.SelectedIndex = -1;
        var next = Ui.ButtonAccent("Tiếp tục", 130); next.Location = new Point(20, 108); next.Enabled = false;
        combo.SelectedIndexChanged += (_, _) => next.Enabled = combo.SelectedIndex >= 0;
        next.DialogResult = DialogResult.OK;
        var cancel = Ui.ButtonOutline("Hủy", 100); cancel.Location = new Point(165, 108); cancel.DialogResult = DialogResult.Cancel;
        chooser.Controls.AddRange([label, combo, next, cancel]); chooser.AcceptButton = next; chooser.CancelButton = cancel;
        if (chooser.ShowDialog(FindForm()) != DialogResult.OK || combo.SelectedValue == null) return;
        using var form = createForm(Convert.ToInt32(combo.SelectedValue));
        if (form.ShowDialog(FindForm()) == DialogResult.OK) TaiLai();
    }

    private void TaiLai()
    {
        _gridDe.DataSource = _service.LayDanhSachDe();
        Ui.RenameColumns(_gridDe,
            ("DeThiID", "ID"), ("TenDe", "Tên đề"), ("TenMon", "Môn"), ("LoaiDe", "Loại đề"),
            ("SoLuongCauHoi", "Số câu"), ("HinhThucTao", "Cách tạo"), ("SoDotThi", "Số đợt thi"));
        Ui.HideColumns(_gridDe, "DeThiID");
        if (_gridDe.Columns.Contains("LoaiDe"))
            foreach (DataGridViewRow row in _gridDe.Rows)
                row.Cells["LoaiDe"].Value = row.Cells["LoaiDe"].Value switch
                {
                    "OnTap" => "Bài tập về nhà",
                    "ThiThu" => "Thi thử",
                    "ChinhThuc" => "Chính thức",
                    var x => x
                };
        TaiDotThi();
    }

    private int DeIdDangChon() =>
        _gridDe.CurrentRow != null && _gridDe.CurrentRow.Cells["DeThiID"].Value != null
            ? Convert.ToInt32(_gridDe.CurrentRow.Cells["DeThiID"].Value)
            : 0;

    private void TaiDotThi()
    {
        var id = DeIdDangChon();
        _gridDot.DataSource = id == 0 ? new DataTable() : _service.LayDanhSachDotThi(id);
        Ui.RenameColumns(_gridDot,
            ("DotThiID", "ID"), ("TenDotThi", "Tên đợt thi"), ("TenDe", "Đề"),
            ("ThoiGianMoCong", "Mở công"), ("ThoiGianDongCong", "Đóng công"),
            ("ThoiLuongLamBai", "Thời lượng (phút)"), ("GioiHanSoLuong", "Giới hạn SL"),
            ("CongBoDiemSom", "Công bố điểm sớm"), ("TrangThai", "Trạng thái"),
            ("PhamVi", "Phạm vi"), ("SoLanThiToiDa", "Lần thi tối đa"),
            ("SoDangKy", "Đã đăng ký"), ("SoBaiDaNop", "Bài đã nộp"));
        Ui.HideColumns(_gridDot, "DotThiID", "TenDe");
    }

    private int DotIdDangChon() =>
        _gridDot.CurrentRow != null && _gridDot.CurrentRow.Cells["DotThiID"].Value != null
            ? Convert.ToInt32(_gridDot.CurrentRow.Cells["DotThiID"].Value)
            : 0;

    private void TaoDotThi()
    {
        var deId = DeIdDangChon();
        if (deId == 0) { MessageBox.Show("Hãy chọn một đề thi phía trên.", "Thông báo"); return; }
        var tenDe = _gridDe.CurrentRow!.Cells["TenDe"].Value?.ToString() ?? "";
        using var f = new frmDotThiEdit(deId, tenDe);
        if (f.ShowDialog(FindForm()) == DialogResult.OK) TaiLai();
    }

    private void DongDot()
    {
        var id = DotIdDangChon();
        if (id == 0) { MessageBox.Show("Hãy chọn một đợt thi.", "Thông báo"); return; }
        if (MessageBox.Show("Đóng đợt thi này ngay? Thí sinh sẽ không thể vào thi.", "Xác nhận",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        _service.CapNhatTrangThaiDot(id, "DaDong");
        TaiDotThi();
    }

    private void XoaDe()
    {
        var id = DeIdDangChon();
        if (id == 0) { MessageBox.Show("Hãy chọn một đề thi.", "Thông báo"); return; }
        var ten = _gridDe.CurrentRow!.Cells["TenDe"].Value;
        if (MessageBox.Show($"Xóa đề '{ten}' cùng toàn bộ đợt thi, đăng ký và bài làm liên quan?\nHành động này KHÔNG THỂ hoàn tác!",
                "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        _service.XoaDe(id);
        TaiLai();
    }

    private void MoManHinhTronDe()
    {
        var deId = DeIdDangChon();
        if (deId == 0) { MessageBox.Show("Hãy chọn một đề thi phía trên để thực hiện trộn đề.", "Thông báo"); return; }
        var tenDe = _gridDe.CurrentRow!.Cells["TenDe"].Value?.ToString() ?? "";
        using var f = new frmTronDe(deId, tenDe);
        f.ShowDialog(FindForm());
        TaiLai();
    }
}
