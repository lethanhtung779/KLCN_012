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
        Text = "Đề thi & Kỳ thi";
        Font = Ui.Body;

        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 56,
            Padding = new Padding(16, 10, 0, 0),
            BackColor = Color.White,
            WrapContents = false
        };
        var btnTuDong = Ui.ButtonAccent("⚡ Tạo đề tự động", 180);
        btnTuDong.Click += (s, e) => { using var f = new frmTaoDeTuDong(); if (f.ShowDialog(FindForm()) == DialogResult.OK) TaiLai(); };
        var btnThuCong = Ui.ButtonPrimary("✎ Tạo đề thủ công", 190);
        btnThuCong.Click += (s, e) => { using var f = new frmTaoDeThuCong(); if (f.ShowDialog(FindForm()) == DialogResult.OK) TaiLai(); };
        var btnDotThi = Ui.ButtonPrimary("📅 Tạo đợt thi", 160);
        btnDotThi.Click += (s, e) => TaoDotThi();
        var btnXoaDe = Ui.ButtonDanger("🗑 Xóa đề", 110);
        btnXoaDe.Click += (s, e) => XoaDe();

        foreach (var c in new Control[] { btnTuDong, btnThuCong, btnDotThi, btnXoaDe })
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
}
