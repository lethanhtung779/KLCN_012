using System.Data;
using System.Reflection;
using QLThiTN.WinForms.Services;
using QLThiTN.WinForms.Models;
using QLThiTN.WinForms.Forms.Common;
using QLThiTN.WinForms.Forms.Auth;
using QLThiTN.WinForms.Forms.Admin;
using QLThiTN.WinForms.Forms.Teacher;

static class DesktopProbes
{
    public static void Run(string connection, string output, int teacherId, int teacherAccountId, int topicId, int subjectId, int examId, bool visible = false)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try { Execute(connection, output, teacherId, teacherAccountId, topicId, subjectId, examId, visible); }
            catch (Exception ex) { failure = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA); thread.Start();
        if (!thread.Join(TimeSpan.FromSeconds(180))) throw new TimeoutException("Desktop audit exceeded 180 seconds.");
        if (failure != null) throw new Exception("Desktop probes failed", failure);
    }
    static void Execute(string connection, string output, int teacherId, int teacherAccountId, int topicId, int subjectId, int examId, bool visible)
    {
        typeof(Database).GetField("_connectionString", BindingFlags.NonPublic | BindingFlags.Static)!.SetValue(null, connection);
        void Note(string id, string text) => WorkflowProbes.Record(id, text);
        void Probe(string id, Action action)
        {
            try { action(); }
            catch (Exception ex)
            {
                Note(id, $"Exception: {ex.GetType().Name}: {ex.Message}");
                if (id is "UI-admin-training-overview" or "UI-teacher-statistics-no-assignment" || id.StartsWith("FLOW-")) throw;
            }
        }
        void Capture(string name, Func<Form> factory)
        {
            Probe("UI-" + name, () =>
            {
                using var form = factory();
                form.ShowInTaskbar = visible;
                form.Opacity = visible ? 1 : 0;
                if (visible) form.StartPosition = FormStartPosition.CenterScreen;
                form.WindowState = FormWindowState.Normal;
                if (form.FormBorderStyle != FormBorderStyle.FixedDialog) form.ClientSize = new Size(1200, 800);
                form.Show(); form.PerformLayout(); Application.DoEvents();
                if (visible)
                {
                    form.Activate();
                    var until = DateTime.UtcNow.AddMilliseconds(1400);
                    while (DateTime.UtcNow < until)
                    {
                        Application.DoEvents();
                        Thread.Sleep(40);
                    }
                }
                if (name == "admin-training")
                {
                    var tabs = form.Controls.OfType<TabControl>().Single();
                    if (tabs.TabCount != 5 || tabs.TabPages.Cast<TabPage>().Any(p => p.Text == "Phân công GV"))
                        throw new Exception("Training tabs still contain the retired assignment page.");
                }
                if (name == "admin-training-overview")
                {
                    var page = form.Controls.OfType<TabControl>().Single().SelectedTab!;
                    var layout = page.Controls.OfType<TableLayoutPanel>().Single();
                    var split = (SplitContainer)layout.GetControlFromPosition(0, 2)!;
                    var left = split.Panel1.Controls.OfType<TableLayoutPanel>().Single();
                    var grid = left.Controls.OfType<DataGridView>().Single();
                    if (grid.Rows.Count == 0 || grid.Height < 300)
                        throw new Exception("Training overview does not show the existing course.");
                }
                if (name == "admin-training-classes-unassigned")
                {
                    var page = form.Controls.OfType<TabControl>().Single().SelectedTab!;
                    var grid = page.Controls.OfType<TableLayoutPanel>().Single().Controls.OfType<DataGridView>().Single();
                    if (grid.Rows.Count != 1 || grid.Rows[0].Cells["TrangThaiPhanCong"].Value?.ToString() != "Chưa phân công")
                        throw new Exception("Unassigned-class filter did not narrow the list.");
                }
                if (name == "teacher-statistics-no-assignment")
                {
                    var root = form.Controls.OfType<TableLayoutPanel>().Single();
                    var body = (Panel)root.GetControlFromPosition(0, 1)!;
                    var message = body.Controls.OfType<Label>().Single();
                    if (!message.Visible || message.Width < 300 || message.Height < 300 ||
                        !message.Text.Contains("Chưa có đợt thi"))
                        throw new Exception("Teacher cannot see the empty statistics explanation.");
                }
                using var image = new Bitmap(form.Width, form.Height);
                form.DrawToBitmap(image, new Rectangle(Point.Empty, form.Size));
                image.Save(Path.Combine(output, name + ".png"));
                Note("UI-" + name, $"Constructed and rendered {form.Width}x{form.Height}.");
            });
        }
        var accountService = new AccountService();
        var training = new TrainingService();
        Capture("login", () => new frmLogin());
        Probe("FLOW-QUICK-LOGIN", () =>
        {
            var demoId = Database.ExecuteScalarInt(@"
                INSERT INTO TaiKhoan(TenDangNhap, MatKhau, Email, VaiTro, TrangThai, NgayTao)
                VALUES(N'giaovien1', @password, N'giaovien1@example.test', N'GiaoVien', N'HoatDong', SYSDATETIME());
                SELECT CAST(SCOPE_IDENTITY() AS int);",
                p => p.AddWithValue("@password", PasswordHelper.Hash("Abc@123456")));
            Database.Execute("INSERT INTO GiaoVien(TaiKhoanID, HoTen) VALUES(@id, N'Demo teacher')",
                p => p.AddWithValue("@id", demoId));
            using var login = new frmLogin { ShowInTaskbar = false, Opacity = 0 };
            login.Show();
            Application.DoEvents();
            login.Controls.OfType<Button>().Single(b => b.Text == "Giáo viên").PerformClick();
            if (Session.Current?.TenDangNhap != "giaovien1" || !Session.IsGiaoVien)
                throw new Exception("Teacher quick-login button did not log in with seeded credentials.");
            Session.Clear();
            Database.Execute("UPDATE TaiKhoan SET MatKhau = @password WHERE TaiKhoanID = @id", p =>
            {
                p.AddWithValue("@password", PasswordHelper.Hash("123456"));
                p.AddWithValue("@id", demoId);
            });
            using var legacyLogin = new frmLogin { ShowInTaskbar = false, Opacity = 0 };
            legacyLogin.Show();
            Application.DoEvents();
            legacyLogin.Controls.OfType<Button>().Single(b => b.Text == "Giáo viên").PerformClick();
            if (Session.Current?.TenDangNhap != "giaovien1" || !Session.IsGiaoVien)
                throw new Exception("Teacher quick-login button did not log in with legacy demo credentials.");
            Note("FLOW-QUICK-LOGIN", "Teacher demo quick-login succeeds with both seed and legacy credentials.");
        });
        Probe("FLOW-LOGIN", () =>
        {
            var auth = new AuthService();
            var (teacherLogin, _) = auth.Login("teacher", "Test-password-123!");
            if (teacherLogin?.VaiTro != "GiaoVien" || teacherLogin.ProfileID != teacherId)
                throw new Exception("Teacher login did not resolve its profile.");
            var (badLogin, _) = auth.Login("teacher", "wrong-password");
            var (studentLogin, _) = auth.Login("student-a", "Test-password-123!");
            if (badLogin != null || studentLogin != null)
                throw new Exception("Invalid or student credentials opened the desktop application.");
            Note("FLOW-LOGIN", "Teacher login succeeds; wrong password and student desktop login are denied.");
        });
        Session.Set(new TaiKhoan { TaiKhoanID = teacherAccountId, ProfileID = int.MaxValue, VaiTro = "GiaoVien", HoTen = "Unassigned teacher" });
        Capture("teacher-statistics-no-assignment", () => new frmStatistics());
        Session.Set(new TaiKhoan { TaiKhoanID = teacherAccountId, ProfileID = teacherId, VaiTro = "GiaoVien", HoTen = "Audit Teacher" });
        Capture("teacher-main", () => new frmMain());
        Capture("teacher-questions", () => new frmQuestions());
        Capture("teacher-question-editor", () => new frmQuestionEdit(0, subjectId));
        Capture("teacher-statistics", () => new frmStatistics());
        Probe("T01", () =>
        {
            var foreignSubject = Database.ExecuteScalarInt("INSERT INTO MonHoc(TenMon) VALUES(N'Foreign stats subject'); SELECT CAST(SCOPE_IDENTITY() AS int);");
            var foreignExam = Database.ExecuteScalarInt($"INSERT INTO DeThi(TenDe,MonHocID,GiaoVienID,SoLuongCauHoi,LaMienPhi,LoaiDe,HinhThucTao) VALUES(N'Foreign Exam',{foreignSubject},{teacherId},1,1,N'ThiThu',N'ThuCong'); SELECT CAST(SCOPE_IDENTITY() AS int);");
            var foreignDotThi = Database.ExecuteScalarInt($"INSERT INTO DotThi(DeThiID,TenDotThi,ThoiGianMoCong,ThoiGianDongCong,ThoiLuongLamBai,SoLanThiToiDa,CongBoDiemSom,TrangThai,PhamVi) VALUES({foreignExam},N'Foreign DotThi',GETDATE(),DATEADD(hour,1,GETDATE()),30,1,0,N'DangMo',N'ToanTruong'); SELECT CAST(SCOPE_IDENTITY() AS int);");
            var stats = new StatisticsService();
            var sessions = stats.LayDanhSachDotThiChoThongKe();
            bool foreignVisible = false;
            foreach (System.Data.DataRow row in sessions.Rows)
            {
                if ((int)row["DotThiID"] == foreignDotThi) foreignVisible = true;
            }
            bool foreignDenied = false;
            try { stats.LayKetQuaDotThi(foreignDotThi); }
            catch (UnauthorizedAccessException) { foreignDenied = true; }
            Note("T01", $"Teacher reads assigned sessions (foreign visible={foreignVisible}, foreign detail blocked={foreignDenied}).");
        });
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
        });
        Probe("T05", () =>
        {
            Database.Execute($"IF NOT EXISTS (SELECT 1 FROM GiaoVien_MonHoc WHERE GiaoVienID={teacherId} AND MonHocID={subjectId}) INSERT INTO GiaoVien_MonHoc(GiaoVienID, MonHocID) VALUES({teacherId}, {subjectId});");
            var myQuestion = new CauHoi { ChuDeID = topicId, NoiDung = "Teacher 1 question", LoaiCauHoi = LoaiCauHoi.TracNghiem, MucDoKho = "NhanBiet", Diem = 1,
                DapAns = Enumerable.Range(0,4).Select(i => new DapAn { NoiDung = i.ToString(), ThuTu = i, LaDapAnDung = i == 0 }).ToList() };
            var id = new QuestionService().Them(myQuestion);

            var secondTkId = Database.ExecuteScalarInt("INSERT INTO TaiKhoan(TenDangNhap,MatKhau,Email,VaiTro,TrangThai,NgayTao) VALUES(N'second-teacher',N'x',N'second-teacher@example.test',N'GiaoVien',N'HoatDong',SYSDATETIME()); SELECT CAST(SCOPE_IDENTITY() AS int);");
            var secondId = Database.ExecuteScalarInt($"INSERT INTO GiaoVien(TaiKhoanID,HoTen) VALUES({secondTkId},N'Other teacher'); SELECT CAST(SCOPE_IDENTITY() AS int);");
            Database.Execute($"INSERT INTO GiaoVien_MonHoc(GiaoVienID,MonHocID) VALUES({secondId},{subjectId})");

            Session.Set(new TaiKhoan { TaiKhoanID = secondTkId, ProfileID = secondId, VaiTro = "GiaoVien", HoTen = "Other teacher" });
            var otherQuestion = new QuestionService().LayChiTiet(id)!;
            otherQuestion.NoiDung = "Changed by another teacher";
            new QuestionService().Sua(otherQuestion);
            Note("T05", "Different teacher identity edits another teacher's unused question successfully.");
            Session.Set(new TaiKhoan { TaiKhoanID = teacherAccountId, ProfileID = teacherId, VaiTro = "GiaoVien", HoTen = "Audit Teacher" });
        });
        Session.Set(new TaiKhoan { TaiKhoanID = teacherAccountId, ProfileID = teacherId, VaiTro = "GiaoVien", HoTen = "Audit Teacher" });
        Capture("teacher-statistics-with-session", () => new frmStatistics());
        Probe("FLOW-TEACHER-QUESTION", () =>
        {
            var service = new QuestionService();
            var question = new CauHoi
            {
                ChuDeID = topicId, NoiDung = "Workflow question", LoaiCauHoi = LoaiCauHoi.TracNghiem,
                MucDoKho = "NhanBiet", Diem = 1,
                DapAns = Enumerable.Range(0, 4).Select(i => new DapAn
                { NoiDung = "Answer " + i, ThuTu = i, LaDapAnDung = i == 0 }).ToList()
            };
            var id = service.Them(question);
            if (service.LayChiTiet(id)?.NoiDung != "Workflow question") throw new Exception("Question creation failed.");
            service.DoiTrangThai(id, true);
            question = service.LayChiTiet(id)!;
            question.NoiDung = "Workflow question edited";
            service.Sua(question);
            service.DoiTrangThai(id, false);
            if (service.LayChiTiet(id)?.NoiDung != "Workflow question edited" || !service.Xoa(id).Ok)
                throw new Exception("Question edit, visibility or deletion failed.");
            Note("FLOW-TEACHER-QUESTION", "Create, read, hide, edit, show and delete succeeded on assigned subject.");
        });
        Probe("PASS-TEACHER-EXAM", () =>
        {
            try { new ExamService(teacherId).TaoDeThuCong("Forbidden", subjectId, "ThiThu", [1]); Note("FAIL", "Teacher created exam."); }
            catch (UnauthorizedAccessException) { Note("PASS-TEACHER-EXAM", "Teacher creation of exam is denied by service."); }
        });
        var adminTkId = Database.ExecuteScalarInt("INSERT INTO TaiKhoan(TenDangNhap,MatKhau,Email,VaiTro,TrangThai,NgayTao) VALUES(N'audit-admin',N'x',N'audit-admin@example.test',N'QuanTriVien',N'HoatDong',SYSDATETIME()); SELECT CAST(SCOPE_IDENTITY() AS int);");
        var adminProfileId = Database.ExecuteScalarInt($"INSERT INTO QuanTriVien(TaiKhoanID,HoTen) VALUES({adminTkId},N'Audit admin'); SELECT CAST(SCOPE_IDENTITY() AS int);");
        var admin = new TaiKhoan { TaiKhoanID = adminTkId, VaiTro = "QuanTriVien", HoTen = "Audit admin", ProfileID = adminProfileId };
        Session.Set(admin);
        Capture("admin-main", () => new frmMain());
        Capture("admin-accounts", () => new frmAccounts());
        Capture("admin-account-editor", () => new frmAccountEdit());
        Capture("admin-training", () => new frmDaoTao());
        var uiCourse = training.ThemKhoaHoc("Assignment layout course", subjectId,
            DateTime.Today, DateTime.Today.AddMonths(1), "HoatDong");
        var uiClass = training.ThemLopHoc("Assignment layout class", uiCourse.Id);
        training.LuuPhanCong(uiClass.Id, subjectId, teacherId, false);
        training.ThemHocVienVaoLop(uiClass.Id, Database.ExecuteScalarInt("SELECT MIN(HocVienID) FROM HocVien"));
        var unassignedClass = training.ThemLopHoc("Unassigned detail class", uiCourse.Id);
        Probe("FLOW-CLASS-ASSIGNMENT-MARK", () =>
        {
            var list = training.LayDanhSachLopHoc(uiCourse.Id, null);
            var assigned = list.Rows.Cast<System.Data.DataRow>().Single(r => Convert.ToInt32(r["LopHocID"]) == uiClass.Id);
            var unassigned = list.Rows.Cast<System.Data.DataRow>().Single(r => Convert.ToInt32(r["LopHocID"]) == unassignedClass.Id);
            if (assigned["TrangThaiPhanCong"].ToString() != "Đã phân công" ||
                unassigned["TrangThaiPhanCong"].ToString() != "Chưa phân công")
                throw new Exception("Class assignment status is incorrect.");
            Note("FLOW-CLASS-ASSIGNMENT-MARK", "Assigned and unassigned classes have distinct labels.");
        });
        Capture("admin-class-detail", () => new frmChiTietLopHoc(uiClass.Id));
        Capture("admin-class-teachers", () =>
        {
            var form = new frmChiTietLopHoc(uiClass.Id);
            var split = form.Controls.OfType<SplitContainer>().Single();
            var layout = split.Panel2.Controls.OfType<TableLayoutPanel>().Single();
            var tabs = (TabControl)layout.GetControlFromPosition(0, 3)!;
            tabs.SelectedIndex = 1;
            var toolbar = tabs.SelectedTab!.Controls.OfType<TableLayoutPanel>().Single()
                .GetControlFromPosition(0, 0) as FlowLayoutPanel;
            if (toolbar?.Controls.OfType<Button>().Count() != 3)
                throw new Exception("Class teacher tab is missing add/edit/remove actions.");
            return form;
        });
        Session.Set(new TaiKhoan { TaiKhoanID = teacherAccountId, ProfileID = teacherId, VaiTro = "GiaoVien", HoTen = "Audit Teacher" });
        Probe("FLOW-CLASS-DETAIL-ROLE", () =>
        {
            var service = new TrainingService();
            if (service.LayThongTinLopDuocXem(uiClass.Id).Rows.Count != 1 ||
                service.LayHocVienLopDuocXem(uiClass.Id).Rows.Count != 1 ||
                service.LayThongTinLopDuocXem(unassignedClass.Id).Rows.Count != 0)
                throw new Exception("Teacher class detail is missing or exposes an unassigned class.");
            try { service.LayHocVienLopDuocXem(unassignedClass.Id); throw new Exception("Foreign class roster was exposed."); }
            catch (UnauthorizedAccessException) { }
            Note("FLOW-CLASS-DETAIL-ROLE", "Teacher sees assigned class and roster, but not another class.");
        });
        Capture("teacher-class-detail", () => new frmChiTietLopHoc(uiClass.Id));
        Session.Set(admin);
        foreach (var (name, index) in new[] { ("classes", 1), ("students", 2), ("schedule", 3), ("overview", 4) })
            Capture("admin-training-" + name, () =>
            {
                var form = new frmDaoTao();
                form.Controls.OfType<TabControl>().Single().SelectedIndex = index;
                return form;
            });
        Capture("admin-training-classes-unassigned", () =>
        {
            var form = new frmDaoTao();
            form.Controls.OfType<TabControl>().Single().SelectedIndex = 1;
            var filter = (CheckBox)typeof(frmDaoTao).GetField("_chkChuaPhanCong", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(form)!;
            filter.Checked = true;
            return form;
        });
        Capture("admin-course-editor", () => new frmKhoaHocEdit());
        Capture("admin-class-editor", () => new frmLopHocEdit());
        Capture("admin-assignment-editor", () => new frmPhanCongEdit(uiClass.Id));
        Capture("admin-schedule-editor", () => new frmLichHocEdit(lopHocId: uiClass.Id));
        Capture("admin-exams", () => new frmExams());
        Capture("admin-exam-manual", () => new frmTaoDeThuCong(teacherId));
        Capture("admin-exam-automatic", () => new frmTaoDeTuDong(teacherId));
        Capture("admin-exam-matrix", () => new frmTaoDeTHPTQG(teacherId));
        Capture("admin-exam-shuffle", () => new frmTronDe(examId, "Audit exam"));
        var shuffleExamId = 0;
        var auditQuestionIds = new List<int>();
        Probe("FLOW-REGENERATE-FOUR-CODES", () =>
        {
            var questionIds = auditQuestionIds;
            for (var q = 1; q <= 4; q++)
            {
                var mucDo = q <= 2 ? "NhanBiet" : "ThongHieu";
                var id = Database.ExecuteScalarInt($@"
                    INSERT INTO CauHoi(ChuDeID,GiaoVienID,NoiDung,LoaiCauHoi,MucDoKho,TrangThai,Diem,NgayTao)
                    VALUES({topicId},{teacherId},N'Audit shuffle question {q}',N'TracNghiem',N'{mucDo}',N'HoatDong',1,SYSDATETIME());
                    SELECT CAST(SCOPE_IDENTITY() AS int);");
                questionIds.Add(id);
                for (var a = 0; a < 4; a++)
                    Database.Execute($"INSERT INTO DapAn(CauHoiID,NoiDung,ThuTu,LaDapAnDung) VALUES({id},N'Answer {a}',{a},{(a == 0 ? 1 : 0)})");
            }
            var service = new ExamService(teacherId);
            shuffleExamId = service.TaoDeThuCong("Four-code shuffle audit", subjectId, "ThiThu", questionIds).DeThiID;
            var first = service.TronDeThi(shuffleExamId, 4, 101, true, false);
            if (!first.Success) throw new Exception(first.Message);
            string Fingerprint()
            {
                var rows = Database.Query(@"
                    SELECT m.TenMaDe, mc.ThuTu AS CauSo, mc.CauHoiID, md.ThuTu AS DapAnSo, md.DapAnID
                    FROM MaDeThi m JOIN MaDe_CauHoi mc ON mc.MaDeID=m.MaDeID
                    JOIN MaDe_DapAn md ON md.MaDeID=m.MaDeID AND md.CauHoiID=mc.CauHoiID
                    WHERE m.DeThiID=@id ORDER BY m.TenMaDe,mc.ThuTu,md.ThuTu",
                    p => p.AddWithValue("@id", shuffleExamId));
                return string.Join("|", rows.AsEnumerable().Select(r => string.Join(":", r.ItemArray)));
            }
            var before = Fingerprint();
            var duplicate = service.TronDeThi(shuffleExamId, 4, 101, false, false);
            if (duplicate.Success || before != Fingerprint())
                throw new Exception("Sinh thêm mã trùng tên đã ghi đè mã cũ.");
            var second = service.TronDeThi(shuffleExamId, 4, 101, true, false);
            if (!second.Success || service.LayDanhSachMaDe(shuffleExamId).Rows.Count != 4 || before == Fingerprint())
                throw new Exception("Regenerating four codes did not produce new permutations.");
            Note("FLOW-REGENERATE-FOUR-CODES", "Four codes 101–104 persist and regenerate with a new order.");
        });
        Capture("admin-exam-shuffle-four", () => new frmTronDe(shuffleExamId, "Four-code shuffle audit"));
        Probe("FLOW-SHUFFLE-BY-TOPIC", () =>
        {
            var secondTopicId = Database.ExecuteScalarInt($"INSERT INTO ChuDe(MonHocID,TenChuDe) VALUES({subjectId},N'Second shuffle topic'); SELECT CAST(SCOPE_IDENTITY() AS int);");
            var otherIds = new List<int>();
            foreach (var level in new[] { "NhanBiet", "ThongHieu" })
            {
                var id = Database.ExecuteScalarInt($@"
                    INSERT INTO CauHoi(ChuDeID,GiaoVienID,NoiDung,LoaiCauHoi,MucDoKho,TrangThai,Diem,NgayTao)
                    VALUES({secondTopicId},{teacherId},N'Second topic {level}',N'TracNghiem',N'{level}',N'HoatDong',1,SYSDATETIME());
                    SELECT CAST(SCOPE_IDENTITY() AS int);");
                otherIds.Add(id);
                for (var a = 0; a < 4; a++)
                    Database.Execute($"INSERT INTO DapAn(CauHoiID,NoiDung,ThuTu,LaDapAnDung) VALUES({id},N'Answer {a}',{a},{(a == 0 ? 1 : 0)})");
            }
            var service = new ExamService(teacherId);
            var idDe = service.TaoDeThuCong("Topic grouping audit", subjectId, "ThiThu",
                [auditQuestionIds[0], otherIds[0], auditQuestionIds[2], otherIds[1]]).DeThiID;
            var result = service.TronDeThi(idDe, 4, 101, true, false);
            if (!result.Success) throw new Exception(result.Message);
            var rows = Database.Query(@"
                SELECT m.TenMaDe, mc.ThuTu, c.ChuDeID, c.MucDoKho
                FROM MaDeThi m JOIN MaDe_CauHoi mc ON mc.MaDeID=m.MaDeID
                JOIN CauHoi c ON c.CauHoiID=mc.CauHoiID
                WHERE m.DeThiID=@id ORDER BY m.TenMaDe,mc.ThuTu",
                p => p.AddWithValue("@id", idDe));
            var codes = rows.AsEnumerable().GroupBy(r => r["TenMaDe"].ToString()).ToList();
            if (codes.Count != 4 || codes.Any(g =>
            {
                var ordered = g.Select(r => (Topic: Convert.ToInt32(r["ChuDeID"]), Level: r["MucDoKho"].ToString())).ToArray();
                return ordered.Length != 4 ||
                    ordered[0] != (topicId, "NhanBiet") || ordered[1] != (topicId, "ThongHieu") ||
                    ordered[2] != (secondTopicId, "NhanBiet") || ordered[3] != (secondTopicId, "ThongHieu");
            }))
                throw new Exception("Shuffled codes interleaved knowledge topics.");
            Note("FLOW-SHUFFLE-BY-TOPIC", "Four codes keep each topic contiguous and order difficulty within topic.");
        });
        Probe("FLOW-MATRIX-DIFFICULTY", () =>
        {
            var service = new ExamService(teacherId);
            var matrix = new List<MaTranChuDeItem>
            {
                new() { ChuDeID = topicId, TenChuDe = "Audit topic", MucDoKho = "NhanBiet", SoCauPhan1 = 2 },
                new() { ChuDeID = topicId, TenChuDe = "Audit topic", MucDoKho = "ThongHieu", SoCauPhan1 = 2 }
            };
            var rejected = service.TaoDeTheoMaTranTHPTQG("Rejected matrix", subjectId, "ThiThu", matrix,
                soChuDeToiThieu: 2);
            if (rejected.DeThiID != 0) throw new Exception("Minimum topic count was ignored.");
            var unavailable = service.TaoDeTheoMaTranTHPTQG("Unavailable matrix", subjectId, "ThiThu",
                [new() { ChuDeID = topicId, TenChuDe = "Audit topic", MucDoKho = "VanDungCao", SoCauPhan1 = 1 }]);
            if (unavailable.DeThiID != 0) throw new Exception("Unavailable difficulty was accepted.");
            var created = service.TaoDeTheoMaTranTHPTQG("Balanced matrix", subjectId, "ThiThu", matrix,
                soLuongMaDe: 4, soChuDeToiThieu: 1, soCauMoiChuDeToiThieu: 4);
            if (created.DeThiID == 0) throw new Exception(created.Error);
            var rows = Database.Query(@"
                SELECT m.TenMaDe, mc.ThuTu, c.MucDoKho
                FROM MaDeThi m JOIN MaDe_CauHoi mc ON mc.MaDeID = m.MaDeID
                JOIN CauHoi c ON c.CauHoiID = mc.CauHoiID
                WHERE m.DeThiID = @id ORDER BY m.TenMaDe, mc.ThuTu",
                p => p.AddWithValue("@id", created.DeThiID));
            var codes = rows.AsEnumerable().GroupBy(r => r["TenMaDe"].ToString()).ToList();
            if (codes.Count != 4 || codes.Any(g => g.Count() != 4 ||
                g.Take(2).Any(r => r["MucDoKho"].ToString() != "NhanBiet") ||
                g.Skip(2).Any(r => r["MucDoKho"].ToString() != "ThongHieu")))
                throw new Exception("Code generation did not preserve the two difficulty quotas and order.");
            Note("FLOW-MATRIX-DIFFICULTY", "Minimum topics validated; four codes retain 2 recognition and 2 comprehension questions.");
        });
        Capture("admin-session-edit", () => new frmDotThiEdit(examId, "Audit exam"));
        Capture("admin-backup", () => new frmBackupRestore());
        Probe("FLOW-ADMIN-BACKUP", () =>
        {
            var folder = Path.Combine(output, "isolated-backup");
            string? file = null;
            try
            {
                file = new BackupService().SaoLuu(folder);
                if (!File.Exists(file) || new FileInfo(file).Length == 0)
                    throw new Exception("Backup file was not created or is empty.");
                Note("FLOW-ADMIN-BACKUP", "Isolated audit database backup succeeded.");
            }
            finally
            {
                if (file != null && File.Exists(file)) File.Delete(file);
                if (Directory.Exists(folder) && !Directory.EnumerateFileSystemEntries(folder).Any()) Directory.Delete(folder);
            }
        });
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
            var freshTeacherTk = Database.ExecuteScalarInt("INSERT INTO TaiKhoan(TenDangNhap,MatKhau,Email,VaiTro,TrangThai,NgayTao) VALUES(N'fresh-teacher',N'x',N'fresh-teacher@example.test',N'GiaoVien',N'HoatDong',SYSDATETIME()); SELECT CAST(SCOPE_IDENTITY() AS int);");
            var freshTeacherId = Database.ExecuteScalarInt($"INSERT INTO GiaoVien(TaiKhoanID,HoTen) VALUES({freshTeacherTk},N'Fresh Teacher'); SELECT CAST(SCOPE_IDENTITY() AS int);");
            var duplicate = training.LuuPhanCong(classroom.Id, subjectId, freshTeacherId, false);
            var sideEffect = Database.ExecuteScalarInt($"SELECT COUNT(*) FROM GiaoVien_MonHoc WHERE GiaoVienID={freshTeacherId} AND MonHocID={subjectId}");
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
        Probe("PASS-ANCHOR-PINNING", () =>
        {
            var isAnchor1 = ExamService.LaDapAnCoDinhCuoi("Tất cả các đáp án trên đều đúng.");
            var isAnchor2 = ExamService.LaDapAnCoDinhCuoi("Cả A và B đều đúng");
            var isAnchor3 = ExamService.LaDapAnCoDinhCuoi("Không có đáp án nào đúng");
            var isNotAnchor = ExamService.LaDapAnCoDinhCuoi("Tất cả các sinh vật đều có màng tế bào");

            var qId = Database.ExecuteScalarInt($"INSERT INTO CauHoi(ChuDeID, GiaoVienID, NoiDung, LoaiCauHoi, MucDoKho, TrangThai, Diem, NgayTao) VALUES({topicId}, {teacherId}, N'Test anchor question', N'TracNghiem', N'De', N'HoatDong', 1, SYSDATETIME()); SELECT CAST(SCOPE_IDENTITY() AS int);");
            var d1 = Database.ExecuteScalarInt($"INSERT INTO DapAn(CauHoiID, NoiDung, ThuTu, LaDapAnDung) VALUES({qId}, N'Tất cả các đáp án trên đều đúng', 0, 1); SELECT CAST(SCOPE_IDENTITY() AS int);");
            var d2 = Database.ExecuteScalarInt($"INSERT INTO DapAn(CauHoiID, NoiDung, ThuTu, LaDapAnDung) VALUES({qId}, N'Phương án 1', 1, 0); SELECT CAST(SCOPE_IDENTITY() AS int);");
            var d3 = Database.ExecuteScalarInt($"INSERT INTO DapAn(CauHoiID, NoiDung, ThuTu, LaDapAnDung) VALUES({qId}, N'Phương án 2', 2, 0); SELECT CAST(SCOPE_IDENTITY() AS int);");
            var d4 = Database.ExecuteScalarInt($"INSERT INTO DapAn(CauHoiID, NoiDung, ThuTu, LaDapAnDung) VALUES({qId}, N'Phương án 3', 3, 0); SELECT CAST(SCOPE_IDENTITY() AS int);");

            var examService = new ExamService(teacherId);
            var (examId2, _) = examService.TaoDeThuCong("Anchor Test Exam", subjectId, "ThiThu", [qId]);
            examService.TronDeThi(examId2, 1, 101, true, false, ghimDapAnCuoi: true);

            var maDeId = Database.ExecuteScalarInt($"SELECT MaDeID FROM MaDeThi WHERE DeThiID={examId2}");
            var anchorOrder = Database.ExecuteScalarInt($"SELECT ThuTu FROM MaDe_DapAn WHERE MaDeID={maDeId} AND DapAnID={d1}");

            Note("PASS-ANCHOR-PINNING", $"Anchors detected={isAnchor1 && isAnchor2 && isAnchor3 && !isNotAnchor}, anchor option pinned to index 3 (D)={anchorOrder == 3}.");
        });
        Probe("FLOW-ADMIN-ACCOUNT", () =>
        {
            var created = accountService.Them("workflow-student", "workflow-student@example.test", "Workflow Student", "HocVien", "Password123!");
            if (!created.Ok) throw new Exception(created.Error);
            var edited = accountService.Sua(created.TaiKhoanID, "workflow-student-updated@example.test", "Updated Student", "HocVien");
            if (!edited.Ok) throw new Exception(edited.Error);
            accountService.DatLaiMatKhau(created.TaiKhoanID, "NewPassword123!");
            var savedHash = Database.ExecuteScalarString($"SELECT MatKhau FROM TaiKhoan WHERE TaiKhoanID = {created.TaiKhoanID}");
            if (!PasswordHelper.Verify("NewPassword123!", savedHash)) throw new Exception("Password reset failed.");
            if (!accountService.CapQuyenGoi(created.TaiKhoanID, "MienPhi").Ok) throw new Exception("Package update failed.");
            accountService.DoiTrangThai(created.TaiKhoanID, true);
            accountService.DoiTrangThai(created.TaiKhoanID, false);
            if (!accountService.Xoa(created.TaiKhoanID).Ok) throw new Exception("Account deletion failed.");
            Note("FLOW-ADMIN-ACCOUNT", "Create, edit, reset password, set package, lock, unlock and delete succeeded.");
        });
        Probe("FLOW-ADMIN-TRAINING", () =>
        {
            var course = training.ThemKhoaHoc("Workflow course", subjectId, DateTime.Today, DateTime.Today.AddMonths(2), "HoatDong");
            if (!course.Ok) throw new Exception(course.Error);
            var editCourse = training.SuaKhoaHoc(course.Id, "Workflow course edited", subjectId, DateTime.Today, DateTime.Today.AddMonths(2), "HoatDong");
            if (!editCourse.Ok) throw new Exception(editCourse.Error);
            var classroom = training.ThemLopHoc("Workflow class", course.Id);
            if (!classroom.Ok || !training.SuaLopHoc(classroom.Id, "Workflow class edited", course.Id).Ok)
                throw new Exception("Class create or edit failed.");
            var lesson = training.ThemLichHoc(classroom.Id, "Workflow lesson", DateTime.Today.AddDays(40), DateTime.Today.AddDays(40).AddHours(1), "Room A");
            if (!lesson.Ok) throw new Exception(lesson.Error);
            if (!training.SuaLichHoc(lesson.Id, classroom.Id, "Workflow lesson edited", DateTime.Today.AddDays(40), DateTime.Today.AddDays(40).AddHours(2), "Room B").Ok ||
                !training.XoaLichHoc(lesson.Id).Ok)
                throw new Exception("Schedule edit or delete failed.");
            if (!training.LuuPhanCong(classroom.Id, subjectId, teacherId, false).Ok ||
                !training.XoaPhanCong(classroom.Id, subjectId).Ok)
                throw new Exception("Teacher assignment create or delete failed.");
            if (!training.XoaLopHoc(classroom.Id).Ok || !training.XoaKhoaHoc(course.Id).Ok)
                throw new Exception("Class or course deletion failed.");
            Note("FLOW-ADMIN-TRAINING", "Course, class, lesson and teacher assignment create/edit/delete succeeded.");
        });
        Session.Clear();
    }
}
