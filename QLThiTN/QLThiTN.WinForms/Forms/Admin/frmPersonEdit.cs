using System.Data;
using QLThiTN.WinForms.Services;

namespace QLThiTN.WinForms.Forms.Admin;

/// <summary>Biểu mẫu hồ sơ và tài khoản đầy đủ cho học viên hoặc giáo viên.</summary>
public sealed class frmPersonEdit : Form
{
    private readonly PersonManagementService _service = new();
    private readonly PersonRecord _record;
    private readonly Dictionary<string, Control> _profileInputs = [];
    private readonly TextBox _username = Ui.Input(330);
    private readonly TextBox _email = Ui.Input(330);
    private readonly TextBox _password = Ui.Input(330);
    private readonly TextBox _name = Ui.Input(330);
    private readonly TextBox _phone = Ui.Input(330);
    private readonly ComboBox _status = Ui.Combo(330);
    private readonly ComboBox _studentType = Ui.Combo(330);
    private readonly ComboBox _package = Ui.Combo(330);
    private readonly DateTimePicker _birth = new() { Width = 330, Format = DateTimePickerFormat.Custom,
        CustomFormat = "dd/MM/yyyy", ShowCheckBox = true, Font = Ui.Body };
    private readonly CheckedListBox _subjects = new() { Font = Ui.Body, CheckOnClick = true,
        BorderStyle = BorderStyle.FixedSingle, Size = new Size(700, 164) };
    private readonly Label _error = Ui.Label("", Ui.Small, Ui.Danger);

    public int SavedAccountId { get; private set; }

    public frmPersonEdit(string vaiTro, int taiKhoanId = 0)
    {
        Session.YeuCauQuanTriVien();
        _record = taiKhoanId == 0
            ? new PersonRecord { VaiTro = vaiTro }
            : _service.LayChiTiet(vaiTro, taiKhoanId) ?? throw new InvalidOperationException("Không tìm thấy hồ sơ.");
        var student = vaiTro == "HocVien";
        if (!student && vaiTro != "GiaoVien") throw new ArgumentException("Vai trò không hợp lệ.");

        Text = $"{(taiKhoanId == 0 ? "Thêm" : "Sửa")} {(student ? "học viên" : "giáo viên")}";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimumSize = new Size(820, 650);
        // Hiển thị được ngay những trường bổ sung đầu tiên trên màn hình phổ biến,
        // thay vì khiến người dùng tưởng rằng form chưa được cập nhật.
        Size = new Size(900, 760);
        BackColor = Color.White;
        Font = Ui.Body;

        _username.Text = _record.TenDangNhap;
        _username.ReadOnly = taiKhoanId != 0;
        _email.Text = _record.Email;
        _password.UseSystemPasswordChar = true;
        _password.PlaceholderText = taiKhoanId == 0 ? "Tối thiểu 6 ký tự" : "Để trống nếu giữ nguyên mật khẩu";
        _name.Text = _record.HoTen;
        _phone.Text = _record.SoDienThoai ?? "";
        _phone.PlaceholderText = "Ví dụ: 0912345678";
        _status.DropDownStyle = ComboBoxStyle.DropDownList;
        _status.Items.AddRange(["Hoạt động", "Khóa"]);
        _status.SelectedIndex = _record.TrangThai == "Khoa" ? 1 : 0;

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2,
            Margin = Padding.Empty, Padding = Padding.Empty };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
        Controls.Add(root);
        var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.White };
        root.Controls.Add(scroll, 0, 0);
        var footer = new Panel { Dock = DockStyle.Fill, BackColor = Color.White,
            Padding = new Padding(24, 12, 24, 12) };
        root.Controls.Add(footer, 0, 1);

        void AddLabel(string caption, int x, int y)
        {
            var label = Ui.Label(caption, Ui.Body, Ui.Navy);
            label.Location = new Point(x, y);
            label.AutoSize = true;
            scroll.Controls.Add(label);
        }
        void AddInput(Control control, int x, int y)
        {
            control.Location = new Point(x, y);
            scroll.Controls.Add(control);
        }
        var accountTitle = Ui.Label("TÀI KHOẢN ĐĂNG NHẬP", Ui.H2, Ui.Navy);
        accountTitle.Location = new Point(24, 16);
        scroll.Controls.Add(accountTitle);
        AddLabel("Tên đăng nhập *", 24, 54); AddInput(_username, 24, 80);
        AddLabel("Email *", 390, 54); AddInput(_email, 390, 80);
        AddLabel(taiKhoanId == 0 ? "Mật khẩu *" : "Mật khẩu mới (tùy chọn)", 24, 132);
        AddInput(_password, 24, 158);
        AddLabel("Trạng thái tài khoản", 390, 132); AddInput(_status, 390, 158);

        var profileTitle = Ui.Label(student ? "HỒ SƠ HỌC VIÊN" : "HỒ SƠ GIÁO VIÊN", Ui.H2, Ui.Navy);
        profileTitle.Location = new Point(24, 220);
        scroll.Controls.Add(profileTitle);
        AddLabel("Họ và tên *", 24, 258); AddInput(_name, 24, 284);
        AddLabel(taiKhoanId == 0 ? "Số điện thoại *" : "Số điện thoại", 390, 258); AddInput(_phone, 390, 284);

        int contentBottom;
        if (student)
        {
            _studentType.DropDownStyle = ComboBoxStyle.DropDownList;
            _studentType.Items.AddRange(["Học sinh", "Tự do"]);
            _studentType.SelectedIndex = _record.LoaiHocVien switch { "HocSinh" => 0, "TuDo" => 1, _ => -1 };
            _package.DropDownStyle = ComboBoxStyle.DropDownList;
            _package.Items.AddRange(["Miễn phí", "Đóng phí (VIP)"]);
            _package.SelectedIndex = _record.LoaiGoi == "DongPhi" ? 1 : 0;
            _birth.Checked = _record.NgaySinh.HasValue;
            _birth.Value = _record.NgaySinh ?? DateTime.Today.AddYears(-18);
            AddLabel(taiKhoanId == 0 ? "Ngày sinh *" : "Ngày sinh (bỏ chọn nếu chưa rõ)", 24, 336); AddInput(_birth, 24, 362);
            AddLabel("Loại học viên *", 390, 336); AddInput(_studentType, 390, 362);
            AddLabel("Gói sử dụng", 24, 414); AddInput(_package, 24, 440);
            var note = Ui.Label("Việc xếp học viên vào lớp được thực hiện ở Khóa học & Lớp → Xếp học viên.", Ui.Small, Ui.TextMuted);
            note.Location = new Point(24, 490);
            note.AutoSize = true;
            scroll.Controls.Add(note);
            contentBottom = 526;
        }
        else
        {
            AddLabel("Môn phụ trách * (chọn ít nhất một môn)", 24, 336);
            _subjects.Location = new Point(24, 362);
            var subjects = _service.LayDanhSachMonHoc();
            foreach (DataRow row in subjects.Rows)
            {
                var id = Convert.ToInt32(row["MonHocID"]);
                _subjects.Items.Add(new SubjectOption(id, row["TenMon"].ToString() ?? ""),
                    _record.MonHocIds.Contains(id));
            }
            scroll.Controls.Add(_subjects);
            var note = Ui.Label("Các môn đã có câu hỏi hoặc lớp phân công không thể bỏ chọn. Môn phụ trách quyết định quyền xem ngân hàng câu hỏi.",
                Ui.Small, Ui.TextMuted);
            note.Location = new Point(24, 540);
            note.MaximumSize = new Size(710, 0);
            note.AutoSize = true;
            scroll.Controls.Add(note);
            contentBottom = 590;
        }
        if (!student)
        {
            _birth.Checked = _record.NgaySinh.HasValue;
            _birth.Value = _record.NgaySinh ?? DateTime.Today.AddYears(-30);
            AddLabel(taiKhoanId == 0 ? "Ngày sinh *" : "Ngày sinh (bỏ chọn nếu chưa rõ)", 24, contentBottom);
            AddInput(_birth, 24, contentBottom + 26);
            contentBottom += 78;
        }
        var fields = ProfileFields.ForRole(vaiTro);
        var extraLink = new LinkLabel
        {
            Text = $"Xem {fields.Length} thông tin bổ sung ↓",
            AutoSize = true,
            Font = Ui.Body,
            LinkColor = Ui.Accent,
            ActiveLinkColor = Ui.Accent2,
            Location = new Point(490, 220)
        };
        extraLink.LinkClicked += (_, _) => scroll.AutoScrollPosition = new Point(0, contentBottom);
        scroll.Controls.Add(extraLink);

        var extraTitle = Ui.Label("THÔNG TIN BỔ SUNG", Ui.H2, Ui.Navy);
        extraTitle.Location = new Point(24, contentBottom);
        scroll.Controls.Add(extraTitle);
        contentBottom += 38;
        var profileY = contentBottom;
        for (int i = 0; i < fields.Length; i += 2)
        {
            var firstKey = fields[i].Key;
            var section = firstKey switch
            {
                "TinhTrangCha" => "THÔNG TIN CHA / MẸ",
                "NguoiLienHe" => "LIÊN HỆ KHẨN CẤP",
                "TrinhDoHocVan" => "TRÌNH ĐỘ & CÔNG TÁC",
                _ => ""
            };
            if (section.Length > 0)
            {
                var sectionLabel = Ui.Label(section, Ui.H2, Ui.Navy);
                sectionLabel.Location = new Point(24, profileY + 4);
                sectionLabel.AutoSize = true;
                scroll.Controls.Add(sectionLabel);
                profileY += 38;
            }

            for (var column = 0; column < 2 && i + column < fields.Length; column++)
            {
                var (key, caption) = fields[i + column];
                var x = column == 0 ? 24 : 390;
                var currentValue = _record.HoSo.GetValueOrDefault(key, "");
                var required = taiKhoanId == 0 && ProfileFields.IsAlwaysRequired(key);
                var input = CreateProfileInput(key, student, currentValue, required);
                _profileInputs[key] = input;
                AddLabel(required ? caption + " *" : caption, x, profileY);
                AddInput(input, x, profileY + 26);
            }
            profileY += 78;
        }
        ConfigureParentStatus("Cha");
        ConfigureParentStatus("Me");
        scroll.AutoScrollMinSize = new Size(760, profileY + 18);

        _error.AutoSize = false;
        _error.Location = new Point(24, 7);
        _error.Size = new Size(460, 55);
        footer.Controls.Add(_error);
        var save = Ui.ButtonAccent(taiKhoanId == 0 ? "Thêm mới" : "Lưu thay đổi", 130, 40);
        save.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        save.Location = new Point(footer.Width - 278, 15);
        save.Click += (_, _) => Save();
        var cancel = Ui.ButtonOutline("Hủy", 100, 40);
        cancel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        cancel.Location = new Point(footer.Width - 138, 15);
        cancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        footer.Controls.AddRange([save, cancel]);
        footer.Resize += (_, _) =>
        {
            save.Left = footer.ClientSize.Width - 278;
            cancel.Left = footer.ClientSize.Width - 138;
        };
        AcceptButton = save;
        CancelButton = cancel;
    }

    private static Control CreateProfileInput(string key, bool student, string currentValue, bool required)
    {
        string[]? choices = key switch
        {
            "GioiTinh" => ["", "Nam", "Nữ", "Khác", "Không muốn cung cấp"],
            "TinhTrangCha" or "TinhTrangMe" => ["", "Còn sống", "Đã mất", "Không rõ thông tin"],
            "TrinhDoHocVan" when student => ["", "Lớp 10", "Lớp 11", "Lớp 12", "Đã tốt nghiệp THPT", "Trung cấp", "Cao đẳng", "Đại học", "Sau đại học"],
            "TrinhDoHocVan" => ["", "Cao đẳng", "Đại học", "Thạc sĩ", "Tiến sĩ", "Khác"],
            "BangCap" => ["", "Chưa có", "Tốt nghiệp THPT", "Trung cấp", "Cao đẳng", "Cử nhân", "Kỹ sư", "Thạc sĩ", "Tiến sĩ", "Chứng chỉ nghiệp vụ sư phạm", "Khác"],
            "QuanHeLienHe" => ["", "Cha", "Mẹ", "Anh/Chị/Em", "Ông/Bà", "Người giám hộ", "Vợ/Chồng", "Khác"],
            _ => null
        };
        if (choices is null)
        {
            var text = Ui.Input(330);
            text.MaxLength = 500;
            text.Text = currentValue;
            text.PlaceholderText = required ? "Bắt buộc" : "Tùy chọn, có thể bổ sung sau";
            return text;
        }

        var combo = Ui.Combo(330);
        combo.Items.AddRange(choices.Cast<object>().ToArray());
        // Giữ được dữ liệu cũ nếu trước đây người dùng đã nhập một giá trị ngoài danh sách.
        if (!string.IsNullOrWhiteSpace(currentValue) && !combo.Items.Contains(currentValue))
            combo.Items.Add(currentValue);
        combo.SelectedItem = currentValue;
        if (combo.SelectedIndex < 0) combo.SelectedIndex = 0;
        return combo;
    }

    private void ConfigureParentStatus(string parent)
    {
        var status = (ComboBox)_profileInputs["TinhTrang" + parent];
        var name = _profileInputs["HoTen" + parent];
        var phone = _profileInputs["Sdt" + parent];
        void Apply(bool clear)
        {
            var alive = status.Text == "Còn sống";
            var deceased = status.Text == "Đã mất";
            name.Enabled = alive || deceased;
            phone.Enabled = alive;
            if (!clear) return;
            if (!alive) phone.Text = "";
            if (!alive && !deceased) name.Text = "";
        }
        status.SelectedIndexChanged += (_, _) => Apply(true);
        Apply(false);
    }

    private void Save()
    {
        _error.Text = "";
        if (_record.VaiTro == "HocVien" && _studentType.SelectedIndex < 0)
        { _error.Text = "Hãy chọn Học sinh hoặc Tự do cho hồ sơ này."; return; }
        _record.TenDangNhap = _username.Text;
        _record.Email = _email.Text;
        _record.HoTen = _name.Text;
        _record.SoDienThoai = _phone.Text;
        _record.TrangThai = _status.SelectedIndex == 1 ? "Khoa" : "HoatDong";
        _record.NgaySinh = _birth.Checked ? _birth.Value.Date : null;
        foreach (var (key, input) in _profileInputs) _record.HoSo[key] = input.Text;
        if (_record.VaiTro == "HocVien")
        {
            _record.NgaySinh = _birth.Checked ? _birth.Value.Date : null;
            _record.LoaiHocVien = _studentType.SelectedIndex == 1 ? "TuDo" : "HocSinh";
            _record.LoaiGoi = _package.SelectedIndex == 1 ? "DongPhi" : "MienPhi";
        }
        else
            _record.MonHocIds = _subjects.CheckedItems.Cast<SubjectOption>().Select(x => x.Id).ToList();
        var (ok, message, id) = _service.Luu(_record, _password.Text);
        if (!ok) { _error.Text = message; return; }
        SavedAccountId = id;
        if (message != "Đã lưu hồ sơ.")
            MessageBox.Show(this, message, "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        DialogResult = DialogResult.OK;
        Close();
    }

    private sealed record SubjectOption(int Id, string Name)
    {
        public override string ToString() => Name;
    }
}
