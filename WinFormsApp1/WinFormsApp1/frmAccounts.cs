using System;
using System.Drawing;
using System.Windows.Forms;

namespace WinFormsApp1
{
    public partial class frmAccounts : Form
    {
        private DataGridView dgvAccounts;
        private Button btnAdd, btnEdit, btnDelete, btnToggleLock;
        private TextBox txtSearch;
        private ComboBox cbRole;

        public frmAccounts()
        {
            InitializeComponent();
            SetupLayout();
            LoadSampleData();
        }

        private void SetupLayout()
        {
            this.Text = "Quản lý Tài khoản Người dùng";
            this.Size = new Size(850, 500);
            this.StartPosition = FormStartPosition.CenterScreen;

            // Bộ lọc tìm kiếm
            txtSearch = new TextBox { Location = new Point(20, 20), Size = new Size(200, 25) };
            cbRole = new ComboBox { Location = new Point(230, 20), Size = new Size(130, 25) };
            cbRole.Items.AddRange(new string[] { "Tất cả", "Giáo viên", "Thí sinh" });
            cbRole.SelectedIndex = 0;

            this.Controls.Add(txtSearch);
            this.Controls.Add(cbRole);

            // DataGridView hiển thị danh sách
            dgvAccounts = new DataGridView();
            dgvAccounts.Location = new Point(20, 60);
            dgvAccounts.Size = new Size(600, 380);
            dgvAccounts.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvAccounts.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            this.Controls.Add(dgvAccounts);

            // Các nút chức năng bên phải
            int btnX = 640;
            btnAdd = CreateActionButton("Thêm Tài khoản", 60, btnX);
            btnEdit = CreateActionButton("Sửa Thông tin", 110, btnX);
            btnDelete = CreateActionButton("Xóa Tài khoản", 160, btnX);
            btnToggleLock = CreateActionButton("Khóa / Mở Khóa", 210, btnX);
            btnToggleLock.BackColor = Color.LightYellow;
        }

        private Button CreateActionButton(string text, int top, int left)
        {
            Button btn = new Button();
            btn.Text = text;
            btn.Size = new Size(160, 35);
            btn.Location = new Point(left, top);
            this.Controls.Add(btn);
            return btn;
        }

        private void LoadSampleData()
        {
            var table = new System.Data.DataTable();
            table.Columns.Add("Mã ĐN", typeof(string));
            table.Columns.Add("Họ và Tên", typeof(string));
            table.Columns.Add("Vai trò", typeof(string));
            table.Columns.Add("Trạng thái", typeof(string));

            table.Rows.Add("GV001", "Nguyễn Văn A", "Giáo viên", "Hoạt động");
            table.Rows.Add("TS001", "Trần Thị B", "Thí sinh", "Hoạt động");
            table.Rows.Add("TS002", "Lê Văn C", "Thí sinh", "Đã khóa");

            dgvAccounts.DataSource = table;
        }
    }
}