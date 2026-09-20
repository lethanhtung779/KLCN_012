using System;
using System.Drawing;
using System.Windows.Forms;

namespace WinFormsApp1
{
    public partial class frmExams : Form
    {
        private DataGridView dgvExams;
        private Button btnCreateExam, btnConfigExam, btnDeleteExam;
        private TextBox txtExamName;
        private ComboBox cbDuration; // Đổi từ TextBox sang ComboBox
        private DateTimePicker dtpStartTime;
        private ComboBox cbSubject;

        public frmExams()
        {
            InitializeComponent();
            SetupLayout();
            LoadSampleData();
        }

        private void SetupLayout()
        {
            this.Text = "Quản lý Đề thi & Kỳ thi THPT Quốc gia";
            this.Size = new Size(940, 590);
            this.StartPosition = FormStartPosition.CenterScreen;

            // GroupBox Cấu hình kỳ thi
            GroupBox gbConfig = new GroupBox();
            gbConfig.Text = "Thiết lập Kỳ thi & Tạo Đề";
            gbConfig.Location = new Point(15, 15);
            gbConfig.Size = new Size(895, 165);
            gbConfig.Font = new Font("Segoe UI", 10, FontStyle.Regular);

            // --- THIẾT KẾ 2 TẦNG (TẦNG TRÊN LÀ NHÃN CHỮ, TẦNG DƯỚI LÀ Ô NHẬP/CHỌN) ---

            // Cột 1: Tên Kỳ thi
            Label lblName = new Label { Text = "Tên Kỳ thi:", Location = new Point(20, 28), AutoSize = true };
            txtExamName = new TextBox { Location = new Point(20, 53), Size = new Size(180, 25) };

            // Cột 2: Môn thi
            Label lblSub = new Label { Text = "Môn thi:", Location = new Point(220, 28), AutoSize = true };
            cbSubject = new ComboBox { Location = new Point(220, 53), Size = new Size(140, 25) };
            cbSubject.Items.AddRange(new string[] { "Toán học", "Vật lý", "Hóa học", "Sinh học", "Tiếng Anh" });
            cbSubject.SelectedIndex = 0;

            // Cột 3: Thời lượng (Sử dụng ComboBox để chọn thay vì nhập)
            Label lblTime = new Label { Text = "Thời lượng:", Location = new Point(380, 28), AutoSize = true };
            cbDuration = new ComboBox { Location = new Point(380, 53), Size = new Size(120, 25) };
            cbDuration.Items.AddRange(new string[] { "15 phút", "45 phút", "50 phút", "90 phút", "120 phút" });
            cbDuration.SelectedIndex = 3; // Mặc định chọn 90 phút

            // Hàng dưới: Thời gian mở và Nút tạo đề
            Label lblDate = new Label { Text = "Thời gian mở kỳ thi:", Location = new Point(20, 95), AutoSize = true };
            dtpStartTime = new DateTimePicker { Location = new Point(20, 120), Size = new Size(230, 25), Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy HH:mm" };

            // Nút bấm tạo đề tự động
            btnCreateExam = new Button { Text = "🎲 Tạo Đề Tự Động", Location = new Point(680, 112), Size = new Size(195, 38), BackColor = Color.LightGreen };
            btnCreateExam.Cursor = Cursors.Hand;
            btnCreateExam.Click += (s, e) => {
                MessageBox.Show(
                    $"Đã tạo thành công đề thi môn [{cbSubject.SelectedItem}] với thời lượng {cbDuration.SelectedItem}!",
                    "Thành công",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            };

            // Thêm tất cả vào GroupBox
            gbConfig.Controls.Add(lblName);
            gbConfig.Controls.Add(txtExamName);
            gbConfig.Controls.Add(lblSub);
            gbConfig.Controls.Add(cbSubject);
            gbConfig.Controls.Add(lblTime);
            gbConfig.Controls.Add(cbDuration);
            gbConfig.Controls.Add(lblDate);
            gbConfig.Controls.Add(dtpStartTime);
            gbConfig.Controls.Add(btnCreateExam);

            this.Controls.Add(gbConfig);

            // DataGridView hiển thị danh sách kỳ thi
            dgvExams = new DataGridView();
            dgvExams.Location = new Point(15, 195);
            dgvExams.Size = new Size(680, 335);
            dgvExams.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            this.Controls.Add(dgvExams);

            // Nút phụ bên phải
            btnConfigExam = new Button { Text = "Sửa cấu hình", Location = new Point(710, 195), Size = new Size(200, 42) };
            btnConfigExam.Cursor = Cursors.Hand;

            btnDeleteExam = new Button { Text = "Xóa Kỳ thi", Location = new Point(710, 250), Size = new Size(200, 42) };
            btnDeleteExam.Cursor = Cursors.Hand;

            this.Controls.Add(btnConfigExam);
            this.Controls.Add(btnDeleteExam);
        }

        private void LoadSampleData()
        {
            var table = new System.Data.DataTable();
            table.Columns.Add("Mã Kỳ Thi", typeof(string));
            table.Columns.Add("Tên Kỳ Thi", typeof(string));
            table.Columns.Add("Môn", typeof(string));
            table.Columns.Add("Thời lượng", typeof(string));
            table.Columns.Add("Trạng thái", typeof(string));

            table.Rows.Add("KT01", "Thi thử THPTQG Lần 1 - Toán", "Toán học", "90 phút", "Đang mở");
            table.Rows.Add("KT02", "Thi thử THPTQG Lần 1 - Lý", "Vật lý", "50 phút", "Chưa mở");

            dgvExams.DataSource = table;
        }
    }
}