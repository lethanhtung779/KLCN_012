using QLThiTN.WinForms.Services;
using QLThiTN.WinForms.Models;
using QLThiTN.WinForms.Forms.Admin;

internal static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        var start = new DateTime(2028, 1, 1);
        var end = new DateTime(2028, 3, 31);
        var weekdays = new[] { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday };
        var holidays = new[] { new DateTime(2028, 2, 7), new DateTime(2028, 2, 7) };
        var rows = RecurringScheduleService.Generate(start, end, TimeSpan.FromHours(18), TimeSpan.FromHours(20), weekdays, holidays);
        var expected = Enumerable.Range(0, 91).Select(i => start.AddDays(i)).Where(d => weekdays.Contains(d.DayOfWeek) && !holidays.Contains(d)).ToArray();
        if (!rows.Select(r => r.Start.Date).SequenceEqual(expected)) throw new Exception("Three month/leap year/holiday mismatch");
        if (rows.Any(r => r.End-r.Start != TimeSpan.FromHours(2))) throw new Exception("Wrong duration");
        foreach (var invalid in new Action[] {
            () => RecurringScheduleService.Generate(end,start,TimeSpan.FromHours(18),TimeSpan.FromHours(20),weekdays,[]),
            () => RecurringScheduleService.Generate(start,end,TimeSpan.FromHours(20),TimeSpan.FromHours(18),weekdays,[]),
            () => RecurringScheduleService.Generate(start,end,TimeSpan.FromHours(18),TimeSpan.FromHours(20),[],[]) })
        {
            try { invalid(); throw new Exception("Invalid schedule accepted"); }
            catch (InvalidOperationException) { }
        }
        Console.WriteLine($"PASS: {rows.Count} sessions, leap year, holidays, invalid inputs");
        if (args.Length > 0)
        {
            Database.LoadConfig(Path.GetFullPath(args[0]));
            var admin = Database.ExecuteScalarInt("SELECT TOP 1 TaiKhoanID FROM TaiKhoan WHERE VaiTro='QuanTriVien' AND TrangThai='HoatDong'");
            Session.Set(new TaiKhoan { TaiKhoanID=admin, VaiTro="QuanTriVien" });
            var existing = Database.ReadList("SELECT TOP 1 LopHocID,ThoiGianBatDau,ThoiGianKetThuc,ISNULL(DiaDiem,N'') FROM LichHoc ORDER BY LichHocID",
                r => (Id:r.GetInt32(0), Start:r.GetDateTime(1), End:r.GetDateTime(2), Room:r.GetString(3))).First();
            var duplicate = new[] { new ScheduleOccurrence(existing.Start, existing.End) };
            if (!RecurringScheduleService.Preview(existing.Id,duplicate,existing.Room)[0].Contains("Trùng lịch lớp")) throw new Exception("Conflict missed");
            // Must fail before any insert; verifies the save-time recheck.
            try { RecurringScheduleService.Save(existing.Id,duplicate,existing.Room,""); throw new Exception("Duplicate saved"); }
            catch (InvalidOperationException) { }
            Application.EnableVisualStyles();
            using var form = new frmLichHocLap(existing.Id);
            form.Show(); Application.DoEvents();
            IEnumerable<Control> Descendants(Control parent) => parent.Controls.Cast<Control>().SelectMany(c => new[] { c }.Concat(Descendants(c)));
            var previewButton = Descendants(form).OfType<Button>().Single(b => b.Text == "Xem trước lịch");
            previewButton.PerformClick();
            var watch = System.Diagnostics.Stopwatch.StartNew();
            var grid = Descendants(form).OfType<DataGridView>().Single();
            while ((!previewButton.Enabled || grid.Rows.Count == 0) && watch.Elapsed < TimeSpan.FromSeconds(45))
            { Application.DoEvents(); Thread.Sleep(20); }
            if (grid.Rows.Count == 0) throw new Exception("Preview did not populate: " + string.Join(" | ", Descendants(form).OfType<Label>().Select(l => l.Text)));
            form.Refresh(); Application.DoEvents();
            using var bitmap = new Bitmap(form.Width, form.Height);
            form.DrawToBitmap(bitmap,new Rectangle(Point.Empty,form.Size));
            bitmap.Save(Path.Combine(AppContext.BaseDirectory,"recurring-preview.png"));
            form.Close();
            Console.WriteLine("PASS: existing conflict preview, save rejects conflict, form opens");
            Session.Clear();
        }
    }
}
