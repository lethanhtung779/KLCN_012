using System;
using System.Drawing;
using System.Windows.Forms;

namespace WinFormsApp1
{
    public partial class frmQuestions : Form
    {
        private DataGridView dgvQuestions;
        private ComboBox cbSubject, cbLevel;
        private Button btnAdd, btnEdit, btnDelete, btnImportFile;

        public frmQuestions()
        {
            InitializeComponent();
            SetupLayout();
        }

        private void SetupLayout()
        {
            this.Text = "Quản lý Ngân hàng Câu hỏi Trắc nghiệm";
            this.Size = new Size(940, 580);
            this.StartPosition = FormStartPosition.CenterScreen;

            // Lọc môn học (Tăng chiều rộng lên 160)
            cbSubject = new ComboBox { Location = new Point(20, 22), Size = new Size(160, 25) };
            cbSubject.Items.AddRange(new string[] { "Tất cả môn", "Toán học", "Vật lý", "Hóa học", "Sinh học", "Tiếng Anh" });
            cbSubject.SelectedIndex = 0;
            this.Controls.Add(cbSubject);

            // Lọc độ khó (Tăng chiều rộng lên 180 để không bị che chữ "Tất cả mức độ")
            cbLevel = new ComboBox { Location = new Point(195, 22), Size = new Size(180, 25) };
            cbLevel.Items.AddRange(new string[] { "Tất cả mức độ", "Nhận biết", "Thông hiểu", "Vận dụng", "Vận dụng cao" });
            cbLevel.SelectedIndex = 0;
            this.Controls.Add(cbLevel);

            // Nút Nhập từ file Excel/.txt: Làm to lên, màu xanh lá nổi bật và icon/chữ rõ ràng
            btnImportFile = new Button
            {
                Text = "📂 Nhập từ File (Excel / .txt)",
                Location = new Point(660, 20),
                Size = new Size(235, 40),
                BackColor = Color.LightGreen,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            btnImportFile.Cursor = Cursors.Hand;
            btnImportFile.Click += (s, e) => {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.Filter = "Excel Files|*.xlsx;*.xls|Text Files|*.txt";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    MessageBox.Show("Đã tải thành công dữ liệu từ file: " + ofd.FileName, "Thông báo");
                }
            };
            this.Controls.Add(btnImportFile);

            // DataGridView hiển thị câu hỏi
            dgvQuestions = new DataGridView();
            dgvQuestions.Location = new Point(20, 75);
            dgvQuestions.Size = new Size(625, 445);
            dgvQuestions.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            this.Controls.Add(dgvQuestions);

            // Các nút thao tác CRUD bên phải (Được căn chỉnh chiều cao và vị trí đồng bộ)
            int btnX = 660;
            btnAdd = new Button { Text = "Thêm Câu hỏi", Location = new Point(btnX, 85), Size = new Size(235, 40) };
            btnEdit = new Button { Text = "Sửa Câu hỏi", Location = new Point(btnX, 135), Size = new Size(235, 40) };
            btnDelete = new Button { Text = "Xóa Câu hỏi", Location = new Point(btnX, 185), Size = new Size(235, 40) };

            btnAdd.Cursor = Cursors.Hand;
            btnEdit.Cursor = Cursors.Hand;
            btnDelete.Cursor = Cursors.Hand;

            this.Controls.Add(btnAdd);
            this.Controls.Add(btnEdit);
            this.Controls.Add(btnDelete);
        }
    }
}