using System.Reflection;
using QLThiTN.WinForms.Services;
using QLThiTN.WinForms.Models;
using QLThiTN.WinForms.Forms.Common;
using QLThiTN.WinForms.Forms.Admin;
using QLThiTN.WinForms.Forms.Teacher;

static class DesktopProbes
{
    public static void Run(string connection, string output, int teacherId, int teacherAccountId, int topicId, int subjectId, int examId)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try { Execute(connection, output, teacherId, teacherAccountId, topicId, subjectId, examId); }
            catch (Exception ex) { failure = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA); thread.Start();
        if (!thread.Join(TimeSpan.FromSeconds(90))) throw new TimeoutException("Desktop audit exceeded 90 seconds.");
        if (failure != null) throw new Exception("Desktop probes failed", failure);
    }
    static void Execute(string connection, string output, int teacherId, int teacherAccountId, int topicId, int subjectId, int examId)
    {
        typeof(Database).GetField("_connectionString", BindingFlags.NonPublic | BindingFlags.Static)!.SetValue(null, connection);
        void Note(string id, string text) => WorkflowProbes.Record(id, text);
        void Probe(string id, Action action)
        {
            try { action(); }
            catch (Exception ex) { Note(id, $"Exception: {ex.GetType().Name}: {ex.Message}"); }
        }
        void Capture(string name, Func<Form> factory)
        {
            Probe("UI-" + name, () =>
            {
                using var form = factory();
                form.ShowInTaskbar = false; form.Opacity = 0;
                form.WindowState = FormWindowState.Normal;
                if (form.FormBorderStyle != FormBorderStyle.FixedDialog) form.ClientSize = new Size(1200, 800);
                form.Show(); form.PerformLayout(); Application.DoEvents();
                using var image = new Bitmap(form.Width, form.Height);
                form.DrawToBitmap(image, new Rectangle(Point.Empty, form.Size));
                image.Save(Path.Combine(output, name + ".png"));
                Note("UI-" + name, $"Constructed and rendered {form.Width}x{form.Height}.");
            });
        }
        var accountService = new AccountService();
        var training = new TrainingService();
        Session.Set(new TaiKhoan { TaiKhoanID = teacherAccountId, ProfileID = teacherId, VaiTro = "GiaoVien", HoTen = "Audit Teacher" });
        Capture("teacher-main", () => new frmMain());
        Capture("teacher-questions", () => new frmQuestions());
        Capture("teacher-statistics", () => new frmStatistics());
        Probe("T01", () => Note("T01", $"Teacher reads {new StatisticsService().LayDanhSachDotThiChoThongKe().Rows.Count} sessions across system, without assignment filtering."));
        Probe("T02", () =>
        {
            var result = accountService.Them("created-by-teacher", "created-by-teacher@example.test", "Unapproved admin", "QuanTriVien", "Audit123!");
            Note("T02", $"Teacher directly calls admin AccountService.Them: success={result.Ok}, new admin ID={result.TaiKhoanID}.");
        });
        Probe("T03", () =>
        {
            using var form = new frmAccounts();
            Note("T03", "Teacher can construct admin account-management form directly; sidebar hiding is not a service permission boundary.");
        });
        Probe("T04", () =>
        {
            var foreignSubject = Database.ExecuteScalarInt("INSERT INTO MonHoc(TenMon) VALUES(N'Unassigned subject'); SELECT CAST(SCOPE_IDENTITY() AS int);");
            var foreignTopic = Database.ExecuteScalarInt($"INSERT INTO ChuDe(MonHocID,TenChuDe) VALUES({foreignSubject},N'Unassigned topic'); SELECT CAST(SCOPE_IDENTITY() AS int);");
            var question = new CauHoi { ChuDeID = foreignTopic, NoiDung = "Teacher not assigned", LoaiCauHoi = LoaiCauHoi.TracNghiem, MucDoKho = "NhanBiet", Diem = 1,
                DapAns = Enumerable.Range(0,4).Select(i => new DapAn { NoiDung = i.ToString(), ThuTu = i, LaDapAnDung = i == 0 }).ToList() };
            var id = new QuestionService().Them(question);
            Note("T04", $"Teacher creates question in unassigned subject successfully: question ID={id}.");
            var second = accountService.Them("second-teacher", "second-teacher@example.test", "Other teacher", "GiaoVien", "Audit123!");
            var secondId = Database.ExecuteScalarInt($"SELECT GiaoVienID FROM GiaoVien WHERE TaiKhoanID={second.TaiKhoanID}");
            Session.Set(new TaiKhoan { TaiKhoanID = second.TaiKhoanID, ProfileID = secondId, VaiTro = "GiaoVien", HoTen = "Other teacher" });
            var otherQuestion = new QuestionService().LayChiTiet(id)!;
            otherQuestion.NoiDung = "Changed by another teacher";
            new QuestionService().Sua(otherQuestion);
            Note("T05", "Different teacher identity edits another teacher's unused question successfully.");
            Session.Set(new TaiKhoan { TaiKhoanID = teacherAccountId, ProfileID = teacherId, VaiTro = "GiaoVien", HoTen = "Audit Teacher" });
        });
        Probe("PASS-TEACHER-EXAM", () =>
        {
            try { new ExamService(teacherId).TaoDeThuCong("Forbidden", subjectId, "ThiThu", [1]); Note("FAIL", "Teacher created exam."); }
            catch (UnauthorizedAccessException) { Note("PASS-TEACHER-EXAM", "Teacher creation of exam is denied by service."); }
        });
        var admin = accountService.Them("audit-admin", "audit-admin@example.test", "Audit admin", "QuanTriVien", "Audit123!");
        Session.Set(new TaiKhoan { TaiKhoanID = admin.TaiKhoanID, VaiTro = "QuanTriVien", HoTen = "Audit admin", ProfileID = 1 });
        Capture("admin-main", () => new frmMain());
        Capture("admin-accounts", () => new frmAccounts());
        Capture("admin-training", () => new frmDaoTao());
        Capture("admin-exams", () => new frmExams());
        Capture("admin-session-edit", () => new frmDotThiEdit(examId, "Audit exam"));
        Probe("A01", () =>
        {
            try { new ExamService(teacherId).XoaDe(examId); Note("A01", "Delete succeeded."); }
            catch (Exception ex) { Note("A01", $"Delete existing exam throws: {ex.Message}"); }
        });
        Probe("A02", () =>
        {
            var result = training.ThemKhoaHoc("Reversed dates", subjectId, DateTime.Now.AddDays(5), DateTime.Now, "HoatDong");
            Note("A02", $"Create course ending before start: success={result.Ok}, ID={result.Id}.");
        });
        Probe("A03", () =>
        {
            var result = accountService.Them("invalid-email", "not-an-email", "Invalid email", "HocVien", "Audit123!");
            Note("A03", $"Create student with invalid email through service: success={result.Ok}.");
        });
        Probe("A04", () =>
        {
            accountService.DoiTrangThai(admin.TaiKhoanID, true);
            Note("A04", "Admin can lock own account through service (normal UI has a guard).");
        });
        Probe("A05", () =>
        {
            var course = training.ThemKhoaHoc("Valid course", subjectId, DateTime.Now, DateTime.Now.AddMonths(1), "HoatDong");
            var classroom = training.ThemLopHoc("Audit class", course.Id);
            var studentId = Database.ExecuteScalarInt("SELECT MIN(HocVienID) FROM HocVien");
            var enrolled = training.ThemHocVienVaoLop(classroom.Id, studentId);
            var repeated = training.ThemHocVienVaoLop(classroom.Id, studentId);
            Note("PASS-TRAINING", $"Course/class/enrollment successes={course.Ok}/{classroom.Ok}/{enrolled.Ok}; repeated enrollment accepted={repeated.Ok}.");
            var now = DateTime.Now;
            var first = training.ThemLichHoc(classroom.Id, "First", now, now.AddHours(1), "Room A");
            var overlap = training.ThemLichHoc(classroom.Id, "Overlapping", now.AddMinutes(10), now.AddHours(1), "Room A");
            Note("A05", $"Two overlapping class sessions accepted: {first.Ok}/{overlap.Ok}.");
            var assigned = training.LuuPhanCong(classroom.Id, subjectId, teacherId, false);
            var secondTeacherId = Database.ExecuteScalarInt("SELECT MAX(GiaoVienID) FROM GiaoVien");
            var duplicate = training.LuuPhanCong(classroom.Id, subjectId, secondTeacherId, false);
            var sideEffect = Database.ExecuteScalarInt($"SELECT COUNT(*) FROM GiaoVien_MonHoc WHERE GiaoVienID={secondTeacherId} AND MonHocID={subjectId}");
            Note("A06", $"Duplicate assignment rejected={!duplicate.Ok}, but candidate teacher acquired subject mapping={sideEffect > 0}.");
        });
        Probe("A07", () =>
        {
            try { accountService.Them("orphan-account", "orphan@example.test", new string('X', 101), "HocVien", "Audit123!"); }
            catch { }
            var orphan = Database.ExecuteScalarInt("SELECT COUNT(*) FROM TaiKhoan t WHERE TenDangNhap=N'orphan-account' AND NOT EXISTS(SELECT 1 FROM HocVien h WHERE h.TaiKhoanID=t.TaiKhoanID)");
            Note("A07", $"Oversized profile name leaves account without student profile: {orphan} orphan record.");
        });
        Probe("A08", () =>
        {
            var questionId = Database.ExecuteScalarInt("SELECT MIN(CauHoiID) FROM CauHoi");
            try { new ExamService(teacherId).TaoDeThuCong("Partial exam", subjectId, "ThiThu", [questionId, questionId]); }
            catch { }
            var partial = Database.ExecuteScalarInt("SELECT COUNT(*) FROM DeThi d WHERE TenDe=N'Partial exam' AND SoLuongCauHoi<>(SELECT COUNT(*) FROM DeThi_CauHoi c WHERE c.DeThiID=d.DeThiID)");
            Note("A08", $"Duplicate question creation leaves inconsistent partial exam: {partial} record.");
        });
        Session.Clear();
    }
}
