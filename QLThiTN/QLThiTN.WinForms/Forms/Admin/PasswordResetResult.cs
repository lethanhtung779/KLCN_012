using QLThiTN.WinForms.Services;

namespace QLThiTN.WinForms.Forms.Admin;

internal sealed class PasswordResetResult : Form
{
    public PasswordResetResult(string code, string name, string password)
    {
        Text = "Đã đặt lại mật khẩu";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(570, 270);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        BackColor = Color.White;
        var title = Ui.Label($"{code} — {name}", Ui.H2, Ui.Navy);
        title.Location = new Point(24, 22);
        var input = Ui.Input(510);
        input.Text = password;
        input.ReadOnly = true;
        input.Location = new Point(24, 65);
        var note = Ui.Label("Mật khẩu cũ đã được thay thế. Sao chép mật khẩu này và gửi riêng cho đúng người dùng.\nChưa có email tự động. Nhắc người dùng đổi mật khẩu sau khi đăng nhập.", Ui.Body, Ui.TextMuted);
        note.MaximumSize = new Size(510, 0);
        note.Location = new Point(24, 112);
        var copy = Ui.ButtonAccent("Sao chép mật khẩu", 200);
        copy.Location = new Point(24, 208);
        copy.Click += (_, _) =>
        {
            try { Clipboard.SetText(password); copy.Text = "Đã sao chép"; }
            catch { MessageBox.Show(this, "Không truy cập được clipboard. Bạn có thể chọn và sao chép trực tiếp trong ô mật khẩu."); }
        };
        var close = Ui.ButtonOutline("Đóng", 100);
        close.Location = new Point(435, 208);
        close.DialogResult = DialogResult.OK;
        Controls.AddRange([title, input, note, copy, close]);
        AcceptButton = CancelButton = close;
    }
}
