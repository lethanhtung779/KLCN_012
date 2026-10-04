using QLThiTN.WinForms.Models;
using QLThiTN.WinForms.Services;

namespace QLThiTN.WinForms.Forms.Teacher;

/// <summary>Them moi / sua cau hoi — ho tro ca 3 loai: TracNghiem, DungSai, TraLoiNgan.</summary>
public class frmQuestionEdit : Form
{
    private readonly QuestionService _service = new();
    private readonly int _id; // 0 = them moi

    private readonly ComboBox _cboMon = Ui.Combo(300);
    private readonly ComboBox _cboChuDe = Ui.Combo(300);
    private readonly ComboBox _cboLoai = Ui.Combo(300);
    private readonly ComboBox _cboMuc = Ui.Combo(300);
    private readonly NumericUpDown _numDiem = new() { Width = 100, DecimalPlaces = 2, Increment = 0.25m, Minimum = 0.01m, Maximum = 10m, Value = 0.25m, Font = Ui.Body };
    private readonly TextBox _txtNoiDung = new() { Width = 620, Height = 90, Multiline = true, Font = Ui.Body, BorderStyle = BorderStyle.FixedSingle };
    private readonly TextBox _txtGiaiThich = new() { Width = 620, Height = 44, Multiline = true, Font = Ui.Body, BorderStyle = BorderStyle.FixedSingle };

    // Cac truong dap an — hien/an theo loai cau hoi
    private readonly TextBox[] _txtDapAn = [Ui.Input(470), Ui.Input(470), Ui.Input(470), Ui.Input(470)];
    private readonly RadioButton[] _radDung = new RadioButton[4];
    private readonly ComboBox[] _cboDungSai = [Ui.Combo(70), Ui.Combo(70), Ui.Combo(70), Ui.Combo(70)];
    private readonly TextBox _txtDapSo = Ui.Input(300);
    private readonly Panel _panelDapAn = new() { Location = new Point(30, 470), Size = new Size(680, 240), BackColor = Color.Transparent };

    private readonly Label _lblError = Ui.Label("", Ui.Small, Ui.Danger);

    public frmQuestionEdit(int id, int macDinhMon = 0)
    {
        _id = id;
        Text = id == 0 ? "Thêm câu hỏi mới" : $"Sửa câu hỏi #{id}";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false; MinimizeBox = false;
        ClientSize = new Size(760, 780);
        BackColor = Color.White;
        AutoScroll = true;

        var lblMon = Ui.Label("Môn học *", Ui.H2, Ui.Navy);
        foreach (var m in _service.LayMonHoc()) _cboMon.Items.Add(new ComboItem(m.MonHocID, m.TenMon));
        _cboMon.SelectedIndexChanged += (s, e) => { TaiChuDe(); };

        var lblChuDe = Ui.Label("Chủ đề *", Ui.H2, Ui.Navy);
        var lblLoai = Ui.Label("Loại câu hỏi *", Ui.H2, Ui.Navy);
        _cboLoai.Items.Add(new ComboItem(LoaiCauHoi.TracNghiem, "Trắc nghiệm (4 đáp án A–D)"));
        _cboLoai.Items.Add(new ComboItem(LoaiCauHoi.DungSai, "Đúng/Sai (4 ý a–d)"));
        _cboLoai.Items.Add(new ComboItem(LoaiCauHoi.TraLoiNgan, "Trả lời ngắn (nhập đáp số)"));
        _cboLoai.SelectedIndexChanged += (s, e) => { HienThiPanelDapAn(); };

        var lblMuc = Ui.Label("Mức độ khó", Ui.H2, Ui.Navy);
        foreach (var (code, display) in MucDoKho.All) _cboMuc.Items.Add(new ComboItem(code, display));
        _cboMuc.SelectedIndex = 0;

        var lblDiem = Ui.Label("Điểm mỗi câu", Ui.H2, Ui.Navy);
        var lblNoiDung = Ui.Label("Nội dung câu hỏi * (hỗ trợ LaTeX $...$ — website tự render)", Ui.H2, Ui.Navy);
        var lblGiaiThich = Ui.Label("Giải thích đáp án (hiện khi xem lại bài)", Ui.H2, Ui.Navy);

        // Panel dap an: tieu de + cac dong se duoc tao trong HienThiPanelDapAn
        var lblDapAn = Ui.Label("Đáp án *", Ui.H2, Ui.Navy);
        lblDapAn.Location = new Point(30, 430);

        var btnLuu = Ui.ButtonAccent("Lưu câu hỏi", 160, 42);
        Ui.GradientButton(btnLuu, Ui.Accent, Ui.Accent2);
        btnLuu.Location = new Point(30, 724);
        btnLuu.Click += (s, e) => Luu();
        var btnHuy = Ui.ButtonOutline("Hủy", 100, 42);
        btnHuy.Location = new Point(200, 724);
        btnHuy.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

        Controls.AddRange([lblMon, _cboMon, lblChuDe, _cboChuDe, lblLoai, _cboLoai, lblMuc, _cboMuc,
            lblDiem, _numDiem, lblNoiDung, _txtNoiDung, lblGiaiThich, _txtGiaiThich, lblDapAn, _panelDapAn, _lblError, btnLuu, btnHuy]);

        var y = 16;
        lblMon.Location = new Point(30, y); _cboMon.Location = new Point(30, y + 24); y += 60;
        lblChuDe.Location = new Point(30, y); _cboChuDe.Location = new Point(30, y + 24); y += 60;
        lblLoai.Location = new Point(30, y); _cboLoai.Location = new Point(30, y + 24); y += 60;
        lblMuc.Location = new Point(30, y); _cboMuc.Location = new Point(30, y + 24);
        lblDiem.Location = new Point(380, y); _numDiem.Location = new Point(380, y + 24); y += 60;
        lblNoiDung.Location = new Point(30, y); _txtNoiDung.Location = new Point(30, y + 24); y += 110;
        lblGiaiThich.Location = new Point(30, y); _txtGiaiThich.Location = new Point(30, y + 24); y += 84;
        lblDapAn.Location = new Point(30, y - 10);
        _panelDapAn.Location = new Point(30, y + 18);
        _lblError.Location = new Point(30, 706);
        btnLuu.Location = new Point(30, 730);
        btnHuy.Location = new Point(200, 730);

        if (_cboMon.Items.Count > 0) _cboMon.SelectedIndex = 0;
        if (macDinhMon > 0)
            foreach (ComboItem item in _cboMon.Items)
                if ((int)item.Value == macDinhMon) { _cboMon.SelectedItem = item; break; }

        if (id > 0) TaiDuLieu(id);
    }

    private class ComboItem(object value, string text)
    {
        public object Value { get; } = value;
        public override string ToString() => text;
    }

    private void TaiChuDe()
    {
        var save = _cboChuDe.SelectedItem as ComboItem;
        _cboChuDe.Items.Clear();
        foreach (var cd in _service.LayChuDe((int)((ComboItem)_cboMon.SelectedItem!).Value))
            _cboChuDe.Items.Add(new ComboItem(cd.ChuDeID, cd.TenChuDe));
        if (_cboChuDe.Items.Count > 0)
        {
            _cboChuDe.SelectedIndex = 0;
            if (save != null)
                foreach (ComboItem item in _cboChuDe.Items)
                    if ((int)item.Value == (int)save.Value) { _cboChuDe.SelectedItem = item; break; }
        }
    }

    private void HienThiPanelDapAn()
    {
        var loai = (_cboLoai.SelectedItem as ComboItem)?.Value as string ?? LoaiCauHoi.TracNghiem;
        _panelDapAn.Controls.Clear();

        if (loai == LoaiCauHoi.TracNghiem)
        {
            _panelDapAn.Height = 190;
            
            for (var i = 0; i < 4; i++)
            {
                var lbl = Ui.Label($"{(char)('A' + i)}.", Ui.Body, Ui.Accent2);
                lbl.Location = new Point(0, 6 + i * 44);
                _txtDapAn[i].Location = new Point(30, 2 + i * 44);
                _radDung[i] = new RadioButton
                {
                    Text = "Đáp án đúng",
                    Location = new Point(520, 4 + i * 44),
                    AutoSize = true,
                    Font = Ui.Body,
                    ForeColor = Ui.Success
                };
                _panelDapAn.Controls.Add(lbl);
                _panelDapAn.Controls.Add(_txtDapAn[i]);
                _panelDapAn.Controls.Add(_radDung[i]);
            }
        }
        else if (loai == LoaiCauHoi.DungSai)
        {
            _panelDapAn.Height = 190;
            var labels = new[] { "Nội dung ý a:", "Nội dung ý b:", "Nội dung ý c:", "Nội dung ý d:" };
            for (var i = 0; i < 4; i++)
            {
                var lbl = Ui.Label(labels[i], Ui.Body, Ui.Accent2);
                lbl.Location = new Point(0, 6 + i * 44);
                _txtDapAn[i].Location = new Point(110, 2 + i * 44);
                _txtDapAn[i].Width = 330;
                _cboDungSai[i].Items.AddRange(["Đúng", "Sai"]);
                _cboDungSai[i].Location = new Point(460, 3 + i * 44);
                _cboDungSai[i].SelectedIndex = 1;
                _panelDapAn.Controls.Add(lbl);
                _panelDapAn.Controls.Add(_txtDapAn[i]);
                _panelDapAn.Controls.Add(_cboDungSai[i]);
            }
            var hint = Ui.Label("Nhập nội dung từng ý a–d và chọn Đúng/Sai cho mỗi ý.", Ui.Small, Ui.TextMuted);
            hint.Location = new Point(110, 180);
            _panelDapAn.Controls.Add(hint);
        }
        else
        {
            _panelDapAn.Height = 60;
            var lbl = Ui.Label("Đáp số:", Ui.Body, Ui.Accent2);
            lbl.Location = new Point(0, 8);
            _txtDapSo.Location = new Point(80, 4);
            var hint = Ui.Label("So sánh không phân biệt hoa/thường; số 663 = 663.0 = 663,0.", Ui.Small, Ui.TextMuted);
            hint.Location = new Point(80, 34);
            _panelDapAn.Controls.Add(lbl);
            _panelDapAn.Controls.Add(_txtDapSo);
            _panelDapAn.Controls.Add(hint);
        }
    }

    private void TaiDuLieu(int id)
    {
        var cau = _service.LayChiTiet(id);
        if (cau == null) { MessageBox.Show("Không tìm thấy câu hỏi."); Close(); return; }

        foreach (ComboItem item in _cboMon.Items)
            if ((int)item.Value == cau.ChuDeID) { _cboMon.SelectedItem = item; break; }
        TaiChuDe();

        foreach (ComboItem item in _cboLoai.Items)
            if ((string)item.Value == cau.LoaiCauHoi) { _cboLoai.SelectedItem = item; break; }
        foreach (ComboItem item in _cboMuc.Items)
            if ((string)item.Value == cau.MucDoKho) { _cboMuc.SelectedItem = item; break; }
        _numDiem.Value = cau.Diem;
        _txtNoiDung.Text = cau.NoiDung;
        _txtGiaiThich.Text = cau.GiaiThichDapAn ?? "";

        HienThiPanelDapAn();
        if (cau.LoaiCauHoi == LoaiCauHoi.TracNghiem)
        {
            for (var i = 0; i < 4 && i < cau.DapAns.Count; i++)
            {
                _txtDapAn[i].Text = cau.DapAns[i].NoiDung;
                _radDung[i].Checked = cau.DapAns[i].LaDapAnDung;
            }
        }
        else if (cau.LoaiCauHoi == LoaiCauHoi.DungSai)
        {
            for (var i = 0; i < 4 && i < cau.DapAns.Count; i++)
            {
                _txtDapAn[i].Text = cau.DapAns[i].NoiDung;
                _cboDungSai[i].SelectedIndex = cau.DapAns[i].LaDapAnDung ? 0 : 1;
            }
        }
        else if (cau.DapAns.Count > 0)
        {
            _txtDapSo.Text = cau.DapAns[0].NoiDung;
        }
    }

    private void Luu()
    {
        _lblError.Text = "";
        try
        {
            if (_cboChuDe.SelectedItem == null) { _lblError.Text = "⚠ Hãy chọn chủ đề."; return; }
            if (string.IsNullOrWhiteSpace(_txtNoiDung.Text)) { _lblError.Text = "⚠ Chưa nhập nội dung câu hỏi."; return; }

            var loai = (_cboLoai.SelectedItem as ComboItem)?.Value as string ?? LoaiCauHoi.TracNghiem;
            var cau = new CauHoi
            {
                CauHoiID = _id,
                ChuDeID = (int)(_cboChuDe.SelectedItem as ComboItem)!.Value!,
                NoiDung = _txtNoiDung.Text.Trim(),
                LoaiCauHoi = loai,
                MucDoKho = (_cboMuc.SelectedItem as ComboItem)?.Value as string ?? MucDoKho.ChuaXacDinh,
                Diem = _numDiem.Value,
                GiaiThichDapAn = string.IsNullOrWhiteSpace(_txtGiaiThich.Text) ? null : _txtGiaiThich.Text.Trim()
            };

            if (loai == LoaiCauHoi.TracNghiem)
            {
                var coDung = false;
                for (var i = 0; i < 4; i++)
                {
                    if (string.IsNullOrWhiteSpace(_txtDapAn[i].Text))
                    { _lblError.Text = $"⚠ Chưa nhập nội dung đáp án {(char)('A' + i)}."; return; }
                    coDung |= _radDung[i].Checked;
                    cau.DapAns.Add(new DapAn { ThuTu = i, NoiDung = _txtDapAn[i].Text.Trim(), LaDapAnDung = _radDung[i].Checked });
                }
                if (!coDung) { _lblError.Text = "⚠ Hãy chọn một đáp án đúng."; return; }
            }
            else if (loai == LoaiCauHoi.DungSai)
            {
                for (var i = 0; i < 4; i++)
                {
                    if (string.IsNullOrWhiteSpace(_txtDapAn[i].Text))
                    { _lblError.Text = $"⚠ Chưa nhập nội dung ý {(char)('a' + i)}."; return; }
                    cau.DapAns.Add(new DapAn
                    {
                        ThuTu = i,
                        NoiDung = _txtDapAn[i].Text.Trim(),
                        LaDapAnDung = _cboDungSai[i].SelectedIndex == 0
                    });
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(_txtDapSo.Text))
                { _lblError.Text = "⚠ Chưa nhập đáp số cho câu trả lời ngắn."; return; }
                cau.DapAns.Add(new DapAn { ThuTu = 0, NoiDung = _txtDapSo.Text.Trim(), LaDapAnDung = true });
            }

            if (_id == 0) _service.Them(cau);
            else _service.Sua(cau);

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            _lblError.Text = "⚠ Lỗi: " + ex.Message;
        }
    }
}
