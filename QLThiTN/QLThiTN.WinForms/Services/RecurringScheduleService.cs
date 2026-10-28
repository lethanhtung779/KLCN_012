using System.Data;
using Microsoft.Data.SqlClient;

namespace QLThiTN.WinForms.Services;

public record ScheduleOccurrence(DateTime Start, DateTime End);

public static class RecurringScheduleService
{
    public static List<ScheduleOccurrence> Generate(DateTime from, DateTime to, TimeSpan start, TimeSpan end,
        IEnumerable<DayOfWeek> weekdays, IEnumerable<DateTime> holidays)
    {
        from = from.Date; to = to.Date;
        if (to < from || (to - from).TotalDays > 366)
            throw new InvalidOperationException("Chọn khoảng ngày hợp lệ, tối đa 1 năm.");
        if (start < TimeSpan.Zero || end >= TimeSpan.FromDays(1) || end <= start)
            throw new InvalidOperationException("Giờ kết thúc phải sau giờ bắt đầu trong cùng ngày.");
        var days = weekdays.ToHashSet();
        if (days.Count == 0) throw new InvalidOperationException("Hãy chọn ít nhất một thứ trong tuần.");
        var excluded = holidays.Select(d => d.Date).ToHashSet();
        var result = new List<ScheduleOccurrence>();
        for (var date = from; date <= to; date = date.AddDays(1))
            if (days.Contains(date.DayOfWeek) && !excluded.Contains(date))
                result.Add(new(date + start, date + end));
        if (result.Count == 0) throw new InvalidOperationException("Khoảng ngày đã chọn không có buổi học nào.");
        return result;
    }

    // Preview and save use identical conflict rules. Save rechecks inside one transaction.
    public static List<string> Preview(int classId, IReadOnlyList<ScheduleOccurrence> rows, string room)
    {
        Session.YeuCauQuanTriVien();
        using var conn = Database.Open();
        Validate(classId, rows, conn, null);
        return Conflicts(classId, rows, room, conn, null);
    }

    public static void Save(int classId, IReadOnlyList<ScheduleOccurrence> rows, string room, string content)
    {
        Session.YeuCauQuanTriVien();
        using var conn = Database.Open();
        using var tx = conn.BeginTransaction(IsolationLevel.Serializable);
        Validate(classId, rows, conn, tx);
        var errors = Conflicts(classId, rows, room, conn, tx);
        var index = errors.FindIndex(e => e.Length > 0);
        if (index >= 0)
            throw new InvalidOperationException($"Buổi {rows[index].Start:dd/MM/yyyy HH:mm}: {errors[index]}. Chưa lưu buổi nào; hãy xem trước lại.");
        foreach (var row in rows)
        {
            using var cmd = new SqlCommand(@"INSERT INTO LichHoc
                (LopHocID,NoiDung,ThoiGianBatDau,ThoiGianKetThuc,DiaDiem)
                VALUES (@lop,@nd,@bd,@kt,@dd)", conn, tx);
            cmd.Parameters.AddWithValue("@lop", classId);
            cmd.Parameters.AddWithValue("@nd", string.IsNullOrWhiteSpace(content) ? DBNull.Value : content.Trim());
            cmd.Parameters.AddWithValue("@dd", string.IsNullOrWhiteSpace(room) ? DBNull.Value : room.Trim());
            cmd.Parameters.AddWithValue("@bd", row.Start);
            cmd.Parameters.AddWithValue("@kt", row.End);
            cmd.ExecuteNonQuery();
        }
        tx.Commit();
    }

    private static void Validate(int classId, IReadOnlyList<ScheduleOccurrence> rows, SqlConnection conn, SqlTransaction? tx)
    {
        if (rows.Count is < 1 or > 367 || rows.Any(r => r.End <= r.Start))
            throw new InvalidOperationException("Danh sách buổi học không hợp lệ.");
        var sorted = rows.OrderBy(r => r.Start).ToArray();
        if (sorted.Skip(1).Where((r, i) => r.Start < sorted[i].End).Any())
            throw new InvalidOperationException("Các buổi dự kiến trùng thời gian với nhau.");
        using var cmd = new SqlCommand("SELECT COUNT(*) FROM LopHoc WHERE LopHocID=@lop", conn, tx);
        cmd.Parameters.AddWithValue("@lop", classId);
        if (Convert.ToInt32(cmd.ExecuteScalar()) != 1) throw new InvalidOperationException("Lớp học không còn tồn tại.");
    }

    private static List<string> Conflicts(int classId, IReadOnlyList<ScheduleOccurrence> rows, string room,
        SqlConnection conn, SqlTransaction? tx)
    {
        using var cmd = new SqlCommand(@"SELECT l.ThoiGianBatDau,l.ThoiGianKetThuc,
            CASE WHEN l.LopHocID=@lop THEN N'Trùng lịch lớp'
                 WHEN @dd<>N'' AND l.DiaDiem=@dd THEN N'Trùng phòng học'
                 ELSE N'Trùng lịch giáo viên' END
            FROM LichHoc l
            WHERE l.ThoiGianBatDau<@kt AND l.ThoiGianKetThuc>@bd
              AND (l.LopHocID=@lop OR (@dd<>N'' AND l.DiaDiem=@dd)
                OR EXISTS (SELECT 1 FROM PhanCongGiangDay a JOIN PhanCongGiangDay b ON a.GiaoVienID=b.GiaoVienID
                    WHERE a.LopHocID=@lop AND b.LopHocID=l.LopHocID))", conn, tx);
        cmd.Parameters.AddWithValue("@lop", classId);
        cmd.Parameters.AddWithValue("@dd", room.Trim());
        cmd.Parameters.AddWithValue("@bd", rows.Min(r => r.Start));
        cmd.Parameters.AddWithValue("@kt", rows.Max(r => r.End));
        var existing = new List<(DateTime Start, DateTime End, string Reason)>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read()) existing.Add((reader.GetDateTime(0), reader.GetDateTime(1), reader.GetString(2)));
        return rows.Select(r => string.Join("; ", existing.Where(e => e.Start < r.End && e.End > r.Start)
            .Select(e => e.Reason).Distinct())).ToList();
    }
}
