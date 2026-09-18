using System;
using System.Drawing;
using System.Windows.Forms;

namespace WinFormsApp1
{
    public partial class frmStatistics : Form
    {
        private TabControl tabControlStats;
        private TabPage tabScores, tabQuestions;
        private DataGridView dgvScores, dgvQuestionStats;
        private ComboBox cbExamFilter;

        public frmStatistics()
        {
            InitializeComponent();
            SetupLayout();
            LoadSampleData();
        }

        private void SetupLayout()
        {
            this.Text = "Thống kê Kết quả Kỳ thi & Chất lượng Câu hỏi";
            this.Size = new Size(940, 600);
            this.StartPosition = FormStartPosition.CenterScreen;

            // Khung chứa các Tab thống kê
            tabControlStats = new TabControl();
            tabControlStats.Location = new Point(15, 15);
            tabControlStats.Size = new Size(895, 530);
            tabControlStats.Font = new Font("Segoe UI", 10, FontStyle.Regular);

            // ================= TAB 1: THỐNG KÊ ĐIỂM THI =================
            tabScores = new TabPage("Thống kê Điểm theo Kỳ thi");

            // Thiết kế kiểu 2 tầng: Nhãn chữ ở tầng trên (Y = 20), ComboBox ở tầng dưới (Y = 48)
            Label lblFilter = new Label { Text = "Chọn kỳ thi cần xem:", Location = new Point(20, 20), AutoSize = true };

            cbExamFilter = new ComboBox { Location = new Point(20, 48), Size = new Size(350, 25) };
            cbExamFilter.Items.AddRange(new string[] { "Thi thử THPTQG Lần 1 - Toán", "Thi thử THPTQG Lần 1 - Lý" });
            cbExamFilter.SelectedIndex = 0;

            // Bảng dữ liệu điểm thi (được dịch xuống dưới để nhường chỗ cho cụm lọc 2 tầng)
            dgvScores = new DataGridView();
            dgvScores.Location = new Point(20, 85);
            dgvScores.Size = new Size(845, 395);
            dgvScores.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            tabScores.Controls.Add(lblFilter);
            tabScores.Controls.Add(cbExamFilter);
            tabScores.Controls.Add(dgvScores);

            // ================= TAB 2: THỐNG KÊ CÂU HỎI =================
            tabQuestions = new TabPage("Chất lượng Câu hỏi (Tỉ lệ Đúng/Sai)");

            dgvQuestionStats = new DataGridView();
            dgvQuestionStats.Location = new Point(20, 20);
            dgvQuestionStats.Size = new Size(845, 460);
            dgvQuestionStats.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            tabQuestions.Controls.Add(dgvQuestionStats);

            // Thêm các tab vào Control chính
            tabControlStats.Controls.Add(tabScores);
            tabControlStats.Controls.Add(tabQuestions);

            this.Controls.Add(tabControlStats);
        }

        private void LoadSampleData()
        {
            // Dữ liệu mẫu cho bảng Điểm thi
            var tableScores = new System.Data.DataTable();
            tableScores.Columns.Add("SBD", typeof(string));
            tableScores.Columns.Add("Họ và Tên", typeof(string));
            tableScores.Columns.Add("Điểm Số", typeof(double));
            tableScores.Columns.Add("Xếp Loại", typeof(string));

            tableScores.Rows.Add("SBD001", "Nguyễn Văn A", 8.5, "Giỏi");
            tableScores.Rows.Add("SBD002", "Trần Thị B", 6.25, "Khá");
            tableScores.Rows.Add("SBD003", "Lê Văn C", 9.0, "Xuất sắc");
            dgvScores.DataSource = tableScores;

            // Dữ liệu mẫu cho bảng Chất lượng câu hỏi
            var tableQuestions = new System.Data.DataTable();
            tableQuestions.Columns.Add("Mã Câu Hỏi", typeof(string));
            tableQuestions.Columns.Add("Nội dung tóm tắt", typeof(string));
            tableQuestions.Columns.Add("Lượt trả lời", typeof(int));
            tableQuestions.Columns.Add("Tỉ lệ Làm đúng", typeof(string));

            tableQuestions.Rows.Add("Q001", "Tính nguyên hàm của hàm số mũ...", 150, "72%");
            tableQuestions.Rows.Add("Q002", "Tìm giá trị cực đại của hàm đa thức...", 150, "41% (Khó)");
            dgvQuestionStats.DataSource = tableQuestions;
        }
    }
}