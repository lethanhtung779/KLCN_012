using System.Data;
using System.Reflection;
using QLThiTN.WinForms.Services;
using QLThiTN.WinForms.Models;
using QLThiTN.WinForms.Forms.Teacher;
using QLThiTN.WinForms.Forms.Admin;

internal static class Program
{
    private static int checks;
    private static void Check(bool valid, string name)
    { if (!valid) throw new Exception(name); checks++; Console.WriteLine("PASS " + name); }
    private static T Field<T>(object o, string name) => (T)o.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(o)!;
    private static DateTime Date(object o, string name) => (DateTime)o.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(o)!;
    [STAThread]
    private static void Main(string[] args)
    {
        foreach (var email in new[] { "abcd@gmail.", "abc@gmail", "abc", "a@.com", "a@b..com", "a b@gmail.com", "Name <a@gmail.com>", "a@-mail.com", "" })
            Check(!DirectoryRules.ValidEmail(email), "Reject " + email);
        foreach (var email in new[] { "abc@gmail.com", "teacher+1@school.edu.vn", "nguyen.van-a@example.org" })
            Check(DirectoryRules.ValidEmail(email), "Accept " + email);
        Check(DirectoryRules.SearchId(" tk000123 ", "TK") == 123, "Account code is case insensitive");
        Check(DirectoryRules.SearchId("LH000123", "TK") == 0, "Code namespaces are separate");
        Check(DirectoryRules.SearchId(DirectoryRules.ClassCode(1234567), "LH") == 1234567, "Large IDs retain uniqueness");
        Check(new DirectoryRules.RoleOption("GiaoVien").ToString() == "Giáo viên", "Vietnamese role label");
        if (args.Length > 0)
        {
            // Optional integration pass: SELECT queries only; never save a form or change accounts.
            Database.LoadConfig(Path.GetFullPath(args[0]));
            var admin = Database.ExecuteScalarInt("SELECT TOP 1 TaiKhoanID FROM TaiKhoan WHERE VaiTro=N'QuanTriVien' AND TrangThai=N'HoatDong'");
            Session.Set(new TaiKhoan { TaiKhoanID = admin, VaiTro = "QuanTriVien" });
            var accounts = new AccountService();
            var all = accounts.LayDanhSach("", "");
            foreach (DataRow row in all.Rows.Cast<DataRow>().Take(5))
            {
                var found = accounts.LayDanhSach(row["MaTaiKhoan"].ToString(), "");
                Check(found.Rows.Count == 1 && Equals(found.Rows[0]["TaiKhoanID"], row["TaiKhoanID"]), "Exact account lookup");
            }
            var training = new TrainingService();
            foreach (DataRow row in training.LayDanhSachLopHoc(null, "").Rows.Cast<DataRow>().Take(5))
            {
                var found = training.LayLopDuocXem(row["MaLop"].ToString());
                Check(found.Rows.Count == 1 && Equals(found.Rows[0]["LopHocID"], row["LopHocID"]), "Exact class lookup");
            }
            foreach (var role in new[] { "HocVien", "GiaoVien" })
            {
                var service = new PersonManagementService();
                var people = service.LayDanhSach(role, "", "");
                if (people.Rows.Count > 0) Check(service.LayDanhSach(role, people.Rows[0]["MaTaiKhoan"].ToString(), "").Rows.Count == 1, "Person lookup " + role);
            }
            Application.EnableVisualStyles();
            var examId = Database.ExecuteScalarInt("SELECT TOP 1 DeThiID FROM DeThi ORDER BY DeThiID");
            using var form = new frmDotThiEdit(examId, "Đề thi kiểm tra bố cục và thời gian");
            form.Show(); Application.DoEvents();
            var date = Field<DateTimePicker>(form, "_openDate");
            var time = Field<DateTimePicker>(form, "_openTime");
            date.Value = new DateTime(2030, 12, 31); time.Value = new DateTime(2030, 12, 31, 23, 50, 0);
            Check(Date(form, "Closes") == new DateTime(2031, 1, 1, 0, 35, 0), "Duration preset crosses year");
            Field<ComboBox>(form, "_window").SelectedIndex = 2;
            Check(Date(form, "Closes") == Date(form, "Opens").AddDays(1), "One day preset");
            Field<DateTimePicker>(form, "_closeTime").Value = new DateTime(2031, 1, 1, 15, 0, 0);
            Check(Field<ComboBox>(form, "_window").SelectedIndex == 4, "Manual closing time switches preset");
            Check(!date.ShowUpDown && time.ShowUpDown, "Calendar date and time picker");
            Field<ComboBox>(form, "_scope").SelectedIndex = 1;
            Check(Field<CheckedListBox>(form, "_classes").Enabled, "Class scope enables selection");
            if (args.Length > 1)
            {
                Directory.CreateDirectory(args[1]);
                using var bitmap = new Bitmap(form.Width, form.Height);
                form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, form.Size));
                bitmap.Save(Path.Combine(args[1], "exam-form.png"));
            }
            form.Close();
            using var exams = new frmExams { Size = new Size(1500, 850) };
            typeof(frmExams).GetMethod("TaiLai", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(exams, null);
            exams.Show(); exams.PerformLayout(); Application.DoEvents();
            var examGrid = Field<DataGridView>(exams, "_gridDe");
            Check(exams.Text == "Đề thi & Lịch thi", "Exam screen uses business-friendly title");
            Check(examGrid.Columns.Contains("TrangThaiWeb"), "Exam grid explains Web visibility");
            Check(Field<Button>(exams, "_btnDuyet").Text.Contains("duyệt", StringComparison.OrdinalIgnoreCase) ||
                Field<Button>(exams, "_btnDuyet").Text.Contains("nội dung", StringComparison.OrdinalIgnoreCase), "Review action is explicit");
            Check(Field<Button>(exams, "_btnDotThi").Text == "Tạo lịch thi", "Schedule action is explicit");
            if (args.Length > 1)
            {
                Directory.CreateDirectory(args[1]);
                using var bitmap = new Bitmap(exams.Width, exams.Height);
                exams.DrawToBitmap(bitmap, new Rectangle(Point.Empty, exams.Size));
                bitmap.Save(Path.Combine(args[1], "exams-workflow.png"));
            }
            exams.Close();
            using var courses = new frmDaoTao { Size = new Size(1100, 730) };
            courses.Show(); Application.DoEvents();
            var tabs = Field<TabControl>(courses, "_tabs");
            tabs.SelectedIndex = 1; Application.DoEvents();
            var grid = Field<DataGridView>(courses, "_gridLop");
            if (grid.Rows.Count > 1)
            {
                grid.CurrentCell = grid.Rows[1].Cells["TenLop"];
                var selectedId = Convert.ToInt32(grid.Rows[1].Cells["LopHocID"].Value);
                tabs.SelectedIndex = 2; Application.DoEvents();
                Check(Equals(Field<ComboBox>(courses, "_cboLopXep").SelectedValue, selectedId), "Class retained in enrollment tab");
                tabs.SelectedIndex = 3; Application.DoEvents();
                Check(Equals(Field<ComboBox>(courses, "_cboLopLh").SelectedValue, selectedId), "Class retained in timetable tab");
                tabs.SelectedIndex = 1; Application.DoEvents();
                Check(Equals(grid.CurrentRow!.Cells["LopHocID"].Value, selectedId), "Class retained returning to list");
                Check(training.LayTongHopDaoTao(DirectoryRules.ClassCode(selectedId)).All(k => k.LopHocs.Any(l => l.LopHocID == selectedId)), "Summary supports class code");
            }
            tabs.SelectedIndex = 2; Application.DoEvents();
            if (args.Length > 1)
            {
                using var bitmap = new Bitmap(courses.Width, courses.Height);
                courses.DrawToBitmap(bitmap, new Rectangle(Point.Empty, courses.Size));
                bitmap.Save(Path.Combine(args[1], "courses.png"));
            }
            courses.Close();
            using var student = new frmPersonEdit("HocVien");
            Check(!Field<ComboBox>(student, "_studentType").Items.Cast<object>().Any(x => x.ToString() == "Sinh viên"), "Student type excludes university student");
            Session.Clear();
        }
        Console.WriteLine($"{checks} checks passed.");
    }
}
