using QLThiTN.WinForms.Services;

namespace QLThiTN.WinForms.Forms.Admin;

/// <summary>Sửa riêng email đăng nhập, không trộn với hồ sơ cá nhân.</summary>
public sealed class frmAccountEmailEdit : Form
{
    private readonly AccountService _service = new();
    private readonly int _taiKhoanId;
    private readonly TextBox _email = Ui.Input(360);
    private readonly Label _error = Ui.Label("", Ui.Small, Ui.Danger);

    public frmAccountEmailEdit(int taiKhoanId, string tenDangNhap, string email)
    {
        _taiKhoanId = taiKhoanId;
        Text = "Sửa email đăng nhập";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(440, 235);
        BackColor = Color.White;
        Font = Ui.Body;

        var account = Ui.Label("Tài khoản: " + tenDangNhap, Ui.H2, Ui.Navy);
        account.Location = new Point(28, 24);
        account.AutoSize = true;
        var label = Ui.Label("Email dùng để đăng nhập và nhận thông báo *", Ui.Body, Ui.TextMuted);
        label.Location = new Point(28, 67);
        label.AutoSize = true;
        _email.Location = new Point(28, 94);
        _email.Text = email;
        _error.Location = new Point(28, 132);
        _error.Size = new Size(380, 38);

        var save = Ui.ButtonAccent("Lưu email", 120, 40);
        save.Location = new Point(28, 178);
        save.Click += (_, _) => Save();
        var cancel = Ui.ButtonOutline("Hủy", 95, 40);
        cancel.Location = new Point(160, 178);
        cancel.DialogResult = DialogResult.Cancel;

        Controls.AddRange([account, label, _email, _error, save, cancel]);
        AcceptButton = save;
        CancelButton = cancel;
    }

    private void Save()
    {
        var result = _service.SuaEmail(_taiKhoanId, _email.Text);
        if (!result.Ok)
        {
            _error.Text = result.Error;
            return;
        }
        DialogResult = DialogResult.OK;
        Close();
    }
}
