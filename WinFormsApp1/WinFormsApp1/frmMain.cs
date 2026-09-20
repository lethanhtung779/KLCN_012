using System;
using System.Drawing;
using System.Windows.Forms;

namespace WinFormsApp1
{
    public partial class frmMain : Form
    {
        private Button btnAccounts;
        private Button btnQuestions;
        private Button btnExams;
        private Button btnStatistics;
        private Label lblTitle;
        private Panel pnlCenterContainer;
        private Panel pnlHeader; // Thêm thanh header màu sắc phía trên

        public frmMain()
        {
            InitializeComponent();
            SetupLayout();
            this.Resize += FrmMain_Resize;
        }

        private void FrmMain_Resize(object sender, EventArgs e)
        {
            CenterControls();
        }

        private void SetupLayout()
        {
            this.Text = "Hệ thống Quản lý & Thi Trắc nghiệm THPT Quốc gia";
            this.Size = new Size(900, 580);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(700, 500);
            this.BackColor = Color.FromArgb(240, 243, 246); // Màu nền xám sáng hiện đại

            // 1. Thanh tiêu đề Header màu sắc ở phía trên
            pnlHeader = new Panel();
            pnlHeader.Size = new Size(this.Width, 90);
            pnlHeader.Dock = DockStyle.Top;
            pnlHeader.BackColor = Color.FromArgb(0, 102, 204); // Xanh dương chủ đạo
            this.Controls.Add(pnlHeader);

            lblTitle = new Label();
            lblTitle.Text = "HỆ THỐNG QUẢN LÝ THI TRẮC NGHIỆM THPT QUỐC GIA";
            lblTitle.Font = new Font("Segoe UI", 16, FontStyle.Bold);
            lblTitle.ForeColor = Color.White;
            lblTitle.AutoSize = true;
            pnlHeader.Controls.Add(lblTitle);
            // Canh giữa tiêu đề trong header
            lblTitle.Location = new Point((pnlHeader.Width - lblTitle.Width) / 2, 28);

            // 2. Khung chứa các nút bấm (Card container)
            pnlCenterContainer = new Panel();
            pnlCenterContainer.Size = new Size(560, 340);
            pnlCenterContainer.Anchor = AnchorStyles.None;
            this.Controls.Add(pnlCenterContainer);

            // 3. Các nút chức năng được phối màu sắc trực quan, sinh động
            int btnTop = 15;

            // Quản lý tài khoản (Màu xanh dương đậm)
            btnAccounts = CreateNavButton("👥  1. Quản lý Tài khoản (Giáo viên / Thí sinh)", btnTop, Color.FromArgb(41, 128, 185));
            btnAccounts.Click += (s, e) => { new frmAccounts().ShowDialog(); };

            // Quản lý ngân hàng câu hỏi (Màu cam sáng)
            btnQuestions = CreateNavButton("📚  2. Quản lý Ngân hàng Câu hỏi", btnTop + 75, Color.FromArgb(211, 84, 0));
            btnQuestions.Click += (s, e) => { new frmQuestions().ShowDialog(); };

            // Quản lý đề thi & kỳ thi (Màu xanh lá đậm)
            btnExams = CreateNavButton("📝  3. Quản lý Đề thi & Kỳ thi", btnTop + 150, Color.FromArgb(39, 174, 96));
            btnExams.Click += (s, e) => { new frmExams().ShowDialog(); };

            // Thống kê kết quả (Màu tím/hồng sang trọng)
            btnStatistics = CreateNavButton("📊  4. Thống kê Kết quả & Chất lượng", btnTop + 225, Color.FromArgb(142, 68, 173));
            btnStatistics.Click += (s, e) => { new frmStatistics().ShowDialog(); };

            CenterControls();
        }

        private Button CreateNavButton(string text, int top, Color bgColor)
        {
            Button btn = new Button();
            btn.Text = text;
            btn.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            btn.Size = new Size(520, 55);
            btn.Location = new Point((pnlCenterContainer.Width - btn.Width) / 2, top);
            btn.Cursor = Cursors.Hand;

            // Tùy chỉnh giao diện phẳng, màu nền và chữ trắng nổi bật
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.BackColor = bgColor;
            btn.ForeColor = Color.White;

            // Hiệu ứng rê chuột đổi màu nhẹ
            btn.MouseEnter += (s, e) => { btn.BackColor = ControlPaint.Light(bgColor, 0.2f); };
            btn.MouseLeave += (s, e) => { btn.BackColor = bgColor; };

            pnlCenterContainer.Controls.Add(btn);
            return btn;
        }

        private void CenterControls()
        {
            if (pnlHeader != null)
            {
                pnlHeader.Width = this.ClientSize.Width;
                lblTitle.Left = (pnlHeader.Width - lblTitle.Width) / 2;
            }

            if (pnlCenterContainer != null)
            {
                pnlCenterContainer.Left = (this.ClientSize.Width - pnlCenterContainer.Width) / 2;
                pnlCenterContainer.Top = pnlHeader.Height + (this.ClientSize.Height - pnlHeader.Height - pnlCenterContainer.Height) / 2;
            }
        }
    }
}