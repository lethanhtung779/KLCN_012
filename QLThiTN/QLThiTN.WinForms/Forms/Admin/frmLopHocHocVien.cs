using System.Data;
using QLThiTN.WinForms.Models;
using QLThiTN.WinForms.Services;

namespace QLThiTN.WinForms.Forms.Admin;

/// <summary>Quản lý quan hệ lớp học từ hồ sơ của một học viên.</summary>
public sealed class frmLopHocHocVien : Form
{
    private readonly TrainingService _service = new();
    private readonly int _hocVienId;
    private readonly string _hoTen;
    private readonly DataGridView _grid = new();
    private readonly ComboBox _availableClasses = Ui.Combo(560);
    private readonly ComboBox _status = Ui.Combo(170);
    private readonly Label _summary = Ui.Label("", Ui.Small, Ui.TextMuted);

    public frmLopHocHocVien(int hocVienId, string hoTen)
    {
        Session.YeuCauQuanTriVien();
        _hocVienId = hocVienId;
        _hoTen = hoTen;
        Text = "Lớp học của học viên";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(900, 560);
        ClientSize = new Size(1120, 680);
        BackColor = Color.White;
        Font = Ui.Body;

        var title = Ui.Label($"Lớp học của {_hoTen}", Ui.H1, Ui.Navy);
        title.Dock = DockStyle.Top;
        title.Height = 58;
        title.Padding = new Padding(18, 15, 0, 0);

        _availableClasses.DropDownStyle = ComboBoxStyle.DropDownList;
        _availableClasses.DisplayMember = "TenHienThi";
        _availableClasses.ValueMember = "LopHocID";
        _availableClasses.DropDownWidth = 820;
        var add = Ui.ButtonAccent("Thêm vào lớp", 145);
        add.Click += (_, _) => AddClass();
        var addBar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 58,
            WrapContents = false,
            Padding = new Padding(14, 8, 0, 8),
            BackColor = Ui.BgSoft
        };
        var addLabel = Ui.Label("Thêm lớp:", Ui.Body, Ui.TextMuted);
        foreach (Control control in new Control[] { addLabel, _availableClasses, add })
        {
            control.Margin = new Padding(0, 4, 10, 0);
            addBar.Controls.Add(control);
        }

        foreach (var (code, display) in TrangThaiHocVienLop.All)
            _status.Items.Add(new StatusOption(code, display));
        _status.DropDownStyle = ComboBoxStyle.DropDownList;
        _status.SelectedIndex = 0;

        var changeStatus = Ui.ButtonOutline("Đổi trạng thái", 145);
        changeStatus.Click += (_, _) => ChangeStatus();
        var remove = Ui.ButtonDanger("Gỡ khỏi lớp", 125);
        remove.Click += (_, _) => RemoveClass();
        var open = Ui.ButtonPrimary("Mở chi tiết lớp", 145);
        open.Click += (_, _) => OpenClass();
        var refresh = Ui.ButtonOutline("Làm mới", 100);
        refresh.Click += (_, _) => LoadData();
        var close = Ui.ButtonOutline("Đóng", 90);
        close.Click += (_, _) => Close();
        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 56,
            WrapContents = false,
            Padding = new Padding(14, 6, 0, 8),
            BackColor = Color.White
        };
        foreach (Control control in new Control[] { Ui.Label("Trạng thái:", Ui.Body, Ui.TextMuted), _status,
                     changeStatus, remove, open, refresh, close })
        {
            control.Margin = new Padding(0, 3, 9, 0);
            actions.Controls.Add(control);
        }

        Ui.StyleGrid(_grid);
        _grid.Dock = DockStyle.Fill;
        _grid.ReadOnly = true;
        _grid.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) OpenClass(); };

        var footer = new Panel { Dock = DockStyle.Bottom, Height = 38, BackColor = Color.White };
        _summary.Location = new Point(16, 10);
        footer.Controls.Add(_summary);

        Controls.Add(_grid);
        Controls.Add(footer);
        Controls.Add(actions);
        Controls.Add(addBar);
        Controls.Add(title);
        Load += (_, _) => LoadData();
    }

    private async void LoadData()
    {
        _summary.Text = "Đang tải lớp học...";
        try
        {
            var data = await Task.Run(() =>
                (classes: _service.LayLopCuaHocVien(_hocVienId),
                 available: _service.LayLopCoTheThemHocVien(_hocVienId)));
            if (IsDisposed || Disposing) return;
            _grid.DataSource = data.classes;
            Ui.RenameColumns(_grid, ("MaLop", "Mã lớp"), ("TenLop", "Tên lớp"),
                ("TenKhoaHoc", "Khóa học"), ("TenMon", "Môn học"),
                ("GiaoVienPhuTrach", "Giáo viên phụ trách"), ("NgayThamGia", "Ngày tham gia"),
                ("TrangThaiHienThi", "Trạng thái"));
            Ui.HideColumns(_grid, "LopHocID", "TrangThai");
            if (_grid.Columns.Contains("NgayThamGia"))
                _grid.Columns["NgayThamGia"]!.DefaultCellStyle.Format = "dd/MM/yyyy";
            if (_grid.Columns.Contains("MaLop")) _grid.Columns["MaLop"]!.Width = 105;
            if (_grid.Columns.Contains("TenLop")) _grid.Columns["TenLop"]!.Width = 155;
            if (_grid.Columns.Contains("TenKhoaHoc")) _grid.Columns["TenKhoaHoc"]!.Width = 210;
            if (_grid.Columns.Contains("GiaoVienPhuTrach")) _grid.Columns["GiaoVienPhuTrach"]!.Width = 190;

            _availableClasses.DataSource = data.available;
            var active = data.classes.AsEnumerable().Count(r => r["TrangThai"].ToString() == TrangThaiHocVienLop.DangHoc);
            _summary.Text = $"{data.classes.Rows.Count} lớp trong lịch sử · {active} lớp đang học" +
                            " · Tài khoản học viên trung tâm · VIP trung tâm";
        }
        catch (Exception ex)
        {
            if (!IsDisposed && !Disposing) _summary.Text = "Không tải được lớp học: " + ex.Message;
        }
    }

    private int SelectedClassId() =>
        _grid.CurrentRow?.Cells["LopHocID"].Value is { } value ? Convert.ToInt32(value) : 0;

    private void AddClass()
    {
        if (_availableClasses.SelectedValue is not int classId)
        {
            MessageBox.Show("Không còn lớp phù hợp để thêm.", "Thông báo");
            return;
        }
        var result = _service.ThemHocVienVaoLop(classId, _hocVienId);
        if (!result.Ok)
            MessageBox.Show(result.Error, "Không thể thêm", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        else
            LoadData();
    }

    private void ChangeStatus()
    {
        var classId = SelectedClassId();
        if (classId == 0 || _status.SelectedItem is not StatusOption option)
        {
            MessageBox.Show("Hãy chọn một lớp và trạng thái.", "Thông báo");
            return;
        }
        var result = _service.DoiTrangThaiHocVienLop(classId, _hocVienId, option.Code);
        if (!result.Ok)
            MessageBox.Show(result.Error, "Không thể cập nhật", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        else
            LoadData();
    }

    private void RemoveClass()
    {
        var classId = SelectedClassId();
        if (classId == 0) { MessageBox.Show("Hãy chọn một lớp.", "Thông báo"); return; }
        var className = _grid.CurrentRow?.Cells["TenLop"].Value?.ToString() ?? "lớp đã chọn";
        if (MessageBox.Show($"Gỡ {_hoTen} khỏi {className}? Lịch sử tham gia lớp sẽ bị xóa.",
                "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        var result = _service.XoaHocVienKhoiLop(classId, _hocVienId);
        if (!result.Ok)
            MessageBox.Show(result.Error, "Không thể gỡ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        else
            LoadData();
    }

    private void OpenClass()
    {
        var classId = SelectedClassId();
        if (classId == 0) { MessageBox.Show("Hãy chọn một lớp.", "Thông báo"); return; }
        using var detail = new frmChiTietLopHoc(classId);
        detail.ShowDialog(this);
        LoadData();
    }

    private sealed record StatusOption(string Code, string Display)
    {
        public override string ToString() => Display;
    }
}
