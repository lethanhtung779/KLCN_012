using QLThiTN.WinForms.Models;
using QLThiTN.WinForms.Services;

if (args.Length != 1) throw new ArgumentException("Truyền thư mục chứa appsettings.json của WinForms.");
Database.LoadConfig(args[0]);
var adminId = Database.ExecuteScalarInt("SELECT TOP 1 TaiKhoanID FROM TaiKhoan WHERE VaiTro=N'QuanTriVien' AND TrangThai=N'HoatDong' ORDER BY TaiKhoanID");
if (adminId == 0) throw new InvalidOperationException("Không tìm thấy tài khoản quản trị đang hoạt động.");
Session.Set(new TaiKhoan { TaiKhoanID = adminId, VaiTro = "QuanTriVien", TrangThai = "HoatDong" });

var topics = Database.ReadList(@"
    SELECT cd.ChuDeID, m.TenMon, cd.TenChuDe
    FROM ChuDe cd JOIN MonHoc m ON m.MonHocID=cd.MonHocID
    WHERE m.TenMon IN (N'Sinh học',N'Công nghệ')",
    r => new { Id = r.GetInt32(0), Subject = r.GetString(1), Name = r.GetString(2) });
int Topic(string subject, string contains) => topics.Single(x => x.Subject == subject && x.Name.Contains(contains, StringComparison.OrdinalIgnoreCase)).Id;

var human = Topic("Sinh học", "Cơ thể");
var genetics = Topic("Sinh học", "Di truyền");
var metabolism = Topic("Sinh học", "Enzim");
var ecology = Topic("Sinh học", "Sinh thái");
var technology = Topic("Công nghệ", "Công nghiệp");

static CauHoi Mc(int topic, string level, string text, int correct, string explanation, params string[] answers) => new()
{
    ChuDeID = topic,
    NoiDung = text,
    LoaiCauHoi = LoaiCauHoi.TracNghiem,
    MucDoKho = level,
    Diem = 0.25m,
    GiaiThichDapAn = explanation,
    DapAns = answers.Select((answer, index) => new DapAn { ThuTu = index, NoiDung = answer, LaDapAnDung = index == correct }).ToList()
};

static CauHoi Short(int topic, string level, string text, string answer, string explanation) => new()
{
    ChuDeID = topic,
    NoiDung = text,
    LoaiCauHoi = LoaiCauHoi.TraLoiNgan,
    MucDoKho = level,
    Diem = 0.50m,
    GiaiThichDapAn = explanation,
    DapAns = [new DapAn { ThuTu = 0, NoiDung = answer, LaDapAnDung = true }]
};

var questions = new List<CauHoi>
{
    Mc(technology, MucDoKho.ThongHieu,
        "Một xưởng muốn thay đổi tốc độ quay của động cơ không đồng bộ ba pha mà vẫn vận hành êm và tiết kiệm điện. Thiết bị phù hợp nhất để điều khiển động cơ là gì?", 1,
        "Biến tần thay đổi tần số và điện áp cấp cho động cơ, nhờ đó điều chỉnh tốc độ quay hiệu quả.",
        "Máy biến áp cách li", "Biến tần", "Cầu dao tự động", "Tụ bù công suất phản kháng"),

    Mc(human, MucDoKho.NhanBiet,
        "Sau bữa ăn giàu tinh bột, nồng độ glucose máu tăng. Hormone nào được tiết ra nhiều hơn để góp phần đưa glucose máu về mức ổn định?", 0,
        "Insulin làm tăng thu nhận glucose ở tế bào và thúc đẩy tổng hợp glycogen.",
        "Insulin", "Glucagon", "Adrenaline", "Thyroxine"),
    Mc(human, MucDoKho.ThongHieu,
        "Một người có nhịp tim 75 lần/phút và thể tích tâm thu 70 mL. Cung lượng tim của người này xấp xỉ bằng bao nhiêu?", 2,
        "Cung lượng tim = nhịp tim × thể tích tâm thu = 75 × 70 = 5250 mL/phút = 5,25 L/phút.",
        "0,94 L/phút", "1,07 L/phút", "5,25 L/phút", "10,50 L/phút"),
    Mc(human, MucDoKho.NhanBiet,
        "Đặc điểm nào của phế nang giúp trao đổi khí giữa phế nang và mao mạch diễn ra hiệu quả?", 3,
        "Phế nang có thành mỏng, ẩm, diện tích bề mặt lớn và mạng mao mạch dày đặc.",
        "Thành dày và khô", "Không có mao mạch bao quanh", "Diện tích bề mặt nhỏ", "Thành mỏng, ẩm và nhiều mao mạch"),
    Mc(human, MucDoKho.ThongHieu,
        "Tiêm vaccine giúp cơ thể đáp ứng nhanh và mạnh hơn khi gặp lại cùng tác nhân gây bệnh chủ yếu vì cơ thể đã hình thành loại tế bào nào?", 1,
        "Tế bào nhớ duy trì thông tin miễn dịch và tạo đáp ứng thứ cấp nhanh hơn.",
        "Hồng cầu", "Tế bào nhớ", "Tiểu cầu", "Tế bào cơ"),
    Mc(human, MucDoKho.VanDung,
        "Khi cơ thể bị mất nước, nồng độ hormone ADH tăng. Biến đổi trực tiếp nào sau đây thường xảy ra ở thận?", 2,
        "ADH làm tăng tính thấm nước của ống lượn xa và ống góp, tăng tái hấp thu nước và làm nước tiểu cô đặc.",
        "Giảm tái hấp thu nước ở ống góp", "Tăng thải nước và làm loãng nước tiểu", "Tăng tái hấp thu nước ở ống góp", "Ngừng hoàn toàn quá trình lọc ở cầu thận"),

    Mc(genetics, MucDoKho.NhanBiet,
        "Trong cơ chế nhân đôi DNA, mỗi phân tử DNA con gồm một mạch cũ và một mạch mới. Nguyên tắc này được gọi là gì?", 0,
        "Nhân đôi DNA tuân theo nguyên tắc bán bảo tồn.",
        "Bán bảo tồn", "Phân li độc lập", "Trội không hoàn toàn", "Dịch mã"),
    Mc(genetics, MucDoKho.ThongHieu,
        "Một đoạn mạch khuôn DNA có chiều 3'-TAC GGA-5'. Trình tự mRNA được phiên mã từ đoạn này là gì?", 1,
        "mRNA được tổng hợp bổ sung và ngược chiều mạch khuôn: 5'-AUG CCU-3'.",
        "3'-AUG CCU-5'", "5'-AUG CCU-3'", "5'-UAC GGA-3'", "3'-UAC GGA-5'"),
    Mc(genetics, MucDoKho.ThongHieu,
        "Một đột biến thay thế một cặp nucleotide nhưng chuỗi polypeptide không thay đổi. Nguyên nhân phù hợp nhất là gì?", 2,
        "Do tính thoái hóa của mã di truyền, nhiều codon khác nhau có thể cùng mã hóa một amino acid.",
        "Mọi đột biến thay thế đều nằm ngoài gene", "Ribosome tự sửa nucleotide bị thay thế", "Mã di truyền có tính thoái hóa", "DNA không tham gia tổng hợp protein"),
    Mc(genetics, MucDoKho.VanDung,
        "Cho hai cơ thể có kiểu gene AaBb giao phối với nhau; hai cặp gene phân li độc lập và trội hoàn toàn. Xác suất đời con có kiểu hình lặn về cả hai tính trạng là bao nhiêu?", 3,
        "Xác suất aa là 1/4 và bb là 1/4; phân li độc lập nên xác suất aabb là 1/16.",
        "1/4", "3/16", "1/8", "1/16"),
    Mc(genetics, MucDoKho.NhanBiet,
        "Một allele lặn gây bệnh nằm trên vùng không tương đồng của nhiễm sắc thể X. Kiểu gene nào biểu thị một người nam mắc bệnh?", 0,
        "Nam giới chỉ có một nhiễm sắc thể X; allele bệnh trên X được biểu hiện ở kiểu gene XᵃY.",
        "XᵃY", "XᴬY", "XᴬXᵃ", "XᴬXᴬ"),
    Mc(genetics, MucDoKho.VanDung,
        "Một quần thể cân bằng Hardy-Weinberg có tần số allele A bằng 0,7. Tần số kiểu gene Aa của quần thể là bao nhiêu?", 2,
        "q = 1 - 0,7 = 0,3; tần số Aa = 2pq = 2 × 0,7 × 0,3 = 0,42.",
        "0,09", "0,21", "0,42", "0,49"),

    Mc(metabolism, MucDoKho.ThongHieu,
        "Khi nhiệt độ vượt quá xa nhiệt độ tối ưu, hoạt tính của nhiều enzyme giảm mạnh chủ yếu do nguyên nhân nào?", 1,
        "Nhiệt độ quá cao có thể làm biến đổi cấu hình không gian, đặc biệt tại trung tâm hoạt động của enzyme.",
        "Cơ chất tự biến mất", "Cấu hình không gian của enzyme bị biến đổi", "Enzyme chuyển thành lipid", "Số lượng nguyên tử trong cơ chất tăng"),
    Mc(metabolism, MucDoKho.NhanBiet,
        "Phân tử nào được xem là đồng tiền năng lượng trực tiếp phổ biến của tế bào?", 3,
        "ATP cung cấp năng lượng trực tiếp cho nhiều quá trình sống thông qua thủy phân liên kết phosphate cao năng.",
        "DNA", "Cellulose", "Oxygen", "ATP"),
    Mc(metabolism, MucDoKho.ThongHieu,
        "Khí oxygen được giải phóng trong quang hợp ở thực vật có nguồn gốc trực tiếp từ chất nào?", 0,
        "Oxygen giải phóng trong pha sáng có nguồn gốc từ quá trình quang phân li nước.",
        "Nước", "Carbon dioxide", "Glucose", "Diệp lục"),

    Mc(ecology, MucDoKho.ThongHieu,
        "Trong một chuỗi thức ăn, nếu hiệu suất truyền năng lượng trung bình giữa hai bậc dinh dưỡng liên tiếp khoảng 10%, năng lượng sẽ biến đổi thế nào khi đi lên bậc dinh dưỡng cao hơn?", 2,
        "Phần lớn năng lượng bị tiêu hao qua hô hấp và tỏa nhiệt nên năng lượng giảm mạnh qua mỗi bậc dinh dưỡng.",
        "Tăng khoảng 10 lần", "Giữ nguyên", "Giảm còn khoảng một phần mười", "Tăng thêm đúng 10%"),
    Mc(ecology, MucDoKho.NhanBiet,
        "Yếu tố nào sau đây thường tác động phụ thuộc mật độ lên kích thước quần thể?", 1,
        "Cạnh tranh giữa các cá thể tăng khi mật độ quần thể tăng nên là yếu tố phụ thuộc mật độ.",
        "Động đất", "Cạnh tranh cùng loài", "Núi lửa phun", "Bão lớn"),
    Mc(ecology, MucDoKho.ThongHieu,
        "Trên một bề mặt đá mới hình thành chưa có đất, nhóm sinh vật nào thường đóng vai trò tiên phong trong diễn thế nguyên sinh?", 3,
        "Địa y có thể sống trên đá trơ, góp phần phong hóa đá và tạo điều kiện hình thành đất.",
        "Cây gỗ lớn", "Động vật ăn thịt", "Cá nước ngọt", "Địa y"),

    Short(genetics, MucDoKho.VanDung,
        "Một phân tử DNA mạch kép có tổng cộng 2400 nucleotide. Số nucleotide loại A chiếm 20% tổng số nucleotide. Hãy cho biết số nucleotide loại G của phân tử DNA.", "720",
        "A = T = 20% × 2400 = 480. G + C = 2400 - 960 = 1440 và G = C nên G = 720."),
    Short(genetics, MucDoKho.VanDung,
        "Một quần thể cân bằng Hardy-Weinberg gồm 1000 cá thể, tần số allele A là 0,6. Hãy tính số cá thể dị hợp Aa theo lí thuyết.", "480",
        "q = 0,4; tần số Aa = 2pq = 2 × 0,6 × 0,4 = 0,48; số cá thể Aa = 480."),
    Short(metabolism, MucDoKho.ThongHieu,
        "Trong điều kiện không đổi, một lượng enzyme chuyển hóa được 120 phân tử cơ chất trong 4 phút. Tốc độ trung bình là bao nhiêu phân tử cơ chất mỗi phút?", "30",
        "Tốc độ trung bình = 120 / 4 = 30 phân tử mỗi phút."),
    Short(ecology, MucDoKho.VanDung,
        "Sinh vật sản xuất tích lũy 20000 kJ. Hiệu suất truyền năng lượng qua mỗi bậc dinh dưỡng là 10%. Hãy tính năng lượng tích lũy ở sinh vật tiêu thụ bậc hai, đơn vị kJ.", "200",
        "Tiêu thụ bậc một nhận 2000 kJ; tiêu thụ bậc hai nhận 10% của 2000 kJ, bằng 200 kJ.")
};

var service = new QuestionService();
var inserted = new List<int>();
foreach (var question in questions)
{
    try
    {
        var id = service.Them(question);
        inserted.Add(id);
        Console.WriteLine($"INSERTED {id}: {question.LoaiCauHoi} - {question.NoiDung[..Math.Min(70, question.NoiDung.Length)]}");
    }
    catch (InvalidOperationException ex) when (ex.Message.Contains("trùng", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine("SKIPPED DUPLICATE: " + question.NoiDung[..Math.Min(70, question.NoiDung.Length)]);
    }
}

Console.WriteLine($"Inserted {inserted.Count}/{questions.Count} original THPT-format questions.");
Console.WriteLine("IDs: " + string.Join(",", inserted));
