using System.Data;
using System.Diagnostics;
using QLThiTN.WinForms.Models;
using QLThiTN.WinForms.Services;

// Read-only integration check against the configured development database.
Database.LoadConfig(Path.GetFullPath(args[0]));
var users = Database.ReadList(@"SELECT t.TaiKhoanID, t.VaiTro, ISNULL(g.GiaoVienID,0)
    FROM TaiKhoan t LEFT JOIN GiaoVien g ON g.TaiKhoanID=t.TaiKhoanID
    WHERE t.TrangThai=N'HoatDong' AND t.VaiTro IN ('QuanTriVien','GiaoVien')",
    r => new TaiKhoan { TaiKhoanID=r.GetInt32(0), VaiTro=r.GetString(1), ProfileID=r.GetInt32(2) });
if (users.Count == 0) throw new Exception("No active users to verify");
var service = new QuestionService();
int[] Ids(DataTable table) => table.AsEnumerable().Select(r => r.Field<int>("CauHoiID")).ToArray();
foreach (var user in users.GroupBy(u => u.VaiTro).Select(g => g.First()))
{
    Session.Set(user);
    foreach (var duplicates in new[] { false, true })
    foreach (var subject in new[] { 0 }.Concat(service.LayMonHoc().Take(1).Select(m => m.MonHocID)))
    {
        var watch = Stopwatch.StartNew();
        var all = service.TraCuu(subject, 0, "", "", "", duplicates);
        var fullMs = watch.Elapsed.TotalMilliseconds;
        watch.Restart();
        var first = service.TraCuu(subject, 0, "", "", "", duplicates, 0, 100);
        Console.WriteLine($"{user.VaiTro}, subject={subject}, duplicates={duplicates}: full={all.Rows.Count} ({fullMs:F1} ms), page={first.Rows.Count} ({watch.Elapsed.TotalMilliseconds:F1} ms)");
        if (!Ids(first).SequenceEqual(Ids(all).Take(100))) throw new Exception("First page differs");
        if (first.Rows.Count > 0 && Convert.ToInt32(first.Rows[0]["TotalCount"]) != all.Rows.Count) throw new Exception("Wrong count");
        // Small pages exercise boundaries even with small development datasets.
        var size = 7;
        foreach (var page in new[] { 0, 1, all.Rows.Count / size, all.Rows.Count / size + 1 }.Distinct())
        {
            var actual = service.TraCuu(subject, 0, "", "", "", duplicates, page, size);
            if (!Ids(actual).SequenceEqual(Ids(all).Skip(page * size).Take(size))) throw new Exception("Page boundary differs");
        }
        var empty = service.TraCuu(subject, 0, "", "", Guid.NewGuid().ToString(), duplicates, 0);
        if (empty.Rows.Count != 0) throw new Exception("Empty search differs");
    }
}
Session.Clear();
Console.WriteLine("PASS: admin/teacher filters, duplicate filters, page counts and boundaries; no database writes.");
