using System.Data;
using QLThiTN.WinForms.Models;
using QLThiTN.WinForms.Services;

namespace QLThiTN.WinForms.Forms.Teacher;

/// <summary>Hop thoai tao de thu cong: tick chon cau hoi tu ngan hang theo mon.</summary>
public class frmTaoDeThuCong : Form
{
    private readonly QuestionService _questionService = new();
    private readonly ExamService _examService;
    private readonly ComboBox _cboMon = Ui.Combo(280);
    private readonly ComboBox _cboMuc = Ui.Combo(200);
    private readonly TextBox _txtTenDe = Ui.Input(280);
    private readonly ComboBox _cboQuyenThi = Ui.Combo(280);
    private readonly DataGridView _grid = new();
    private readonly Label _lblDaChon = Ui.Label("Đã chọn 0 câu", Ui.H2, Ui.Accent2);
    private readonly Label _lblError = Ui.Label("", Ui.Small, Ui.Danger);
    private bool _dangTaiDuLieu;

    public frmTaoDeThuCong(int? giaoVienId = null)
    {
        _examService = new ExamService(giaoVienId);
        _cboQuyenThi.Items.AddRange(["Chỉ tài khoản VIP", "Miễn phí cho mọi tài khoản"]);
        _cboQuyenThi.SelectedIndex = 0;
        Text = "Tạo đề thi thủ công — chọn câu hỏi từ ngân hàng";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false; MinimizeBox = false;
        ClientSize = new Size(1020, 720);
        BackColor = Color.White;

        var lblMon = Ui.Label("Môn học", Ui.H2, Ui.Navy);
        foreach (var m in _questionService.LayMonHoc()) _cboMon.Items.Add(new ComboItem(m.MonHocID, m.TenMon));
        if (_cboMon.Items.Count > 0) _cboMon.SelectedIndex = 0;
        _cboMon.SelectedIndexChanged += (s, e) => TaiPool();

        var lblMuc = Ui.Label("Độ khó", Ui.H2, Ui.Navy);
        _cboMuc.Items.Add(new ComboItem("", "Tất cả"));
        foreach (var (code, display) in MucDoKho.All) _cboMuc.Items.Add(new ComboItem(code, display));
        _cboMuc.SelectedIndex = 0;
        _cboMuc.SelectedIndexChanged += (s, e) => TaiPool();

        var lblTen = Ui.Label("Tên đề thi *", Ui.H2, Ui.Navy);
        _txtTenDe.Text = $"Đề ôn tập tự chọn - {DateTime.Now:dd/MM/yyyy}";
        var lblQuyenThi = Ui.Label("Quyền làm bài *", Ui.H2, Ui.Navy);
        var btnChonTatCa = Ui.ButtonOutline("☑ Chọn tất cả", 140, 34);
        btnChonTatCa.Click += (s, e) => { foreach (DataGridViewRow row in _grid.Rows) row.Cells["Chon"].Value = true; CapNhatDem(); };
        var btnBoTatCa = Ui.ButtonOutline("☐ Bỏ chọn tất cả", 160, 34);
        btnBoTatCa.Click += (s, e) => { foreach (DataGridViewRow row in _grid.Rows) row.Cells["Chon"].Value = false; CapNhatDem(); };

        Ui.StyleGrid(_grid);
        _grid.Location = new Point(20, 142);
        _grid.Size = new Size(980, 438);
        _grid.ReadOnly = false;
        _grid.CellValueChanged += (s, e) => { if (e.ColumnIndex == 0) CapNhatDem(); };
        _grid.CurrentCellDirtyStateChanged += (s, e) =>
        {
            if (_grid.IsCurrentCellDirty)
            {
                _grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
                CapNhatDem();
            }
        };
        _grid.CellContentClick += (s, e) =>
        {
            if (e.RowIndex >= 0 && _grid.Columns[e.ColumnIndex].Name == "Chon")
            {
                _grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
                CapNhatDem();
            }
        };
        _grid.CellDoubleClick += (s, e) =>
        {
            if (e.RowIndex >= 0 && _grid.Columns.Contains("Chon"))
            {
                var cur = Convert.ToBoolean(_grid.Rows[e.RowIndex].Cells["Chon"].Value ?? false);
                _grid.Rows[e.RowIndex].Cells["Chon"].Value = !cur;
                _grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
                CapNhatDem();
            }
        };
        _grid.KeyDown += (s, e) =>
        {
            if (e.KeyCode == Keys.Space && _grid.CurrentRow != null && _grid.Columns.Contains("Chon"))
            {
                var cur = Convert.ToBoolean(_grid.CurrentRow.Cells["Chon"].Value ?? false);
                _grid.CurrentRow.Cells["Chon"].Value = !cur;
                _grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
                CapNhatDem();
                e.Handled = true;
            }
        };

        var btnTao = Ui.ButtonAccent("✔ Tạo đề thi", 170, 42);
        Ui.GradientButton(btnTao, Ui.Accent, Ui.Accent2);
        btnTao.Click += (s, e) => Tao();
        var btnHuy = Ui.ButtonOutline("Hủy", 100, 42);
        btnHuy.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

        Controls.AddRange([lblMon, _cboMon, lblMuc, _cboMuc, lblTen, _txtTenDe, lblQuyenThi, _cboQuyenThi,
            btnChonTatCa, btnBoTatCa, _grid, _lblDaChon, _lblError, btnTao, btnHuy]);

        lblMon.Location = new Point(20, 16); _cboMon.Location = new Point(20, 40);
        lblMuc.Location = new Point(320, 16); _cboMuc.Location = new Point(320, 40);
        lblTen.Location = new Point(540, 16); _txtTenDe.Location = new Point(540, 40);
        lblQuyenThi.Location = new Point(540, 78); _cboQuyenThi.Location = new Point(540, 100);
        btnChonTatCa.Location = new Point(20, 90);
        btnBoTatCa.Location = new Point(170, 90);
        _lblDaChon.Location = new Point(360, 96);
        _lblError.Location = new Point(20, 596);
        btnTao.Location = new Point(20, 620);
        btnHuy.Location = new Point(200, 620);

        TaiPool();
    }

    private class ComboItem(object value, string text)
    {
        public object Value { get; } = value;
        public override string ToString() => text;
    }

    private void TaiPool()
    {
        if (_cboMon.SelectedItem == null) return;
        _dangTaiDuLieu = true;
        var mon = (int)(_cboMon.SelectedItem as ComboItem)!.Value!;
        var muc = (_cboMuc.SelectedItem as ComboItem)?.Value as string ?? "";
        var dt = _examService.LayCauHoiPool(mon, muc);

        // Them cot chon (checkbox) vao dau
        var chon = new DataColumn("Chon", typeof(bool)) { DefaultValue = false };
        dt.Columns.Add(chon);
        dt.Columns["Chon"].SetOrdinal(0);

        _grid.DataSource = dt;
        _grid.ReadOnly = false;
        foreach (DataGridViewColumn col in _grid.Columns)
            col.ReadOnly = col.Name != "Chon";

        Ui.RenameColumns(_grid,
            ("CauHoiID", "ID"), ("NoiDung", "Nội dung"), ("LoaiCauHoi", "Loại"),
            ("MucDoKho", "Độ khó"), ("Diem", "Điểm"), ("TenChuDe", "Chủ đề"));
        Ui.HideColumns(_grid, "CauHoiID");
        // Dat AutoSizeMode=None truoc Width: dat Width tren cot Fill khi grid chua co handle bi NullReferenceException.
        var colChon = _grid.Columns["Chon"]!;
        colChon.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
        colChon.Width = 65;
        colChon.HeaderText = "Chọn";
        colChon.ReadOnly = false;
        _grid.Columns["NoiDung"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        _dangTaiDuLieu = false;
        CapNhatDem();
    }

    private void CapNhatDem()
    {
        if (_dangTaiDuLieu) return;
        var dem = 0;
        foreach (DataGridViewRow row in _grid.Rows)
            if (Convert.ToBoolean(row.Cells["Chon"].Value ?? false)) dem++;
        _lblDaChon.Text = $"Đã chọn {dem} câu";
    }

    private void Tao()
    {
        _lblError.Text = "";
        if (_cboMon.SelectedItem == null) return;
        var mon = (int)(_cboMon.SelectedItem as ComboItem)!.Value!;
        var ids = new List<int>();
        foreach (DataGridViewRow row in _grid.Rows)
            if (Convert.ToBoolean(row.Cells["Chon"].Value ?? false))
                ids.Add(Convert.ToInt32(row.Cells["CauHoiID"].Value));

        var (id, error) = _examService.TaoDeThuCong(_txtTenDe.Text, mon, "ThiThu", ids,
            _cboQuyenThi.SelectedIndex == 1);
        if (id == 0) { _lblError.Text = "⚠ " + error; return; }
        MessageBox.Show($"Tạo đề thành công! (ID: {id})\nHãy xem và duyệt đề, sau đó tạo lịch thi cho học viên.",
            "Hoàn tất", MessageBoxButtons.OK, MessageBoxIcon.Information);
        DialogResult = DialogResult.OK;
        Close();
    }
}
