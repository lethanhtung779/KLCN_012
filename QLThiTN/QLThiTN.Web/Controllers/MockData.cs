using QLThiTN.Web.ViewModels;

namespace QLThiTN.Web.Controllers;

public static class MockData
{
    public static List<ExamScheduleViewModel> Exams = new()
    {
        new ExamScheduleViewModel
        {
            Id = 1,
            Title = "Kiểm tra 15 phút — Hàm số",
            Description = "Bài kiểm tra nhanh chương hàm số trên lớp Toán buổi tối, bám sát kiến thức đã giảng, giúp giáo viên nắm bắt tình hình tiếp thu của lớp.",
            Duration = 15,
            StartTime = DateTime.Now.AddDays(-1),
            EndTime = DateTime.Now.AddDays(3),
            MaxStudents = 200,
            SubjectName = "Toán",
            QuestionCount = 10,
            HasRegistered = true,
            CanRegister = true,
            ExamKind = ExamKind.QuickQuiz,
            IsAssignedToMe = true,
            ScorePublished = true
        },
        new ExamScheduleViewModel
        {
            Id = 2,
            Title = "Bài tập về nhà — Điện xoay chiều",
            Description = "10 câu trắc nghiệm điện xoay chiều giao tại lớp, nộp trước 21:00 tối thứ Ba để giáo viên chấm trước buổi học sau.",
            Duration = 30,
            StartTime = DateTime.Now.AddDays(-1),
            EndTime = DateTime.Now.AddDays(1),
            MaxStudents = 150,
            SubjectName = "Vật lý",
            QuestionCount = 10,
            HasRegistered = true,
            CanRegister = true,
            ExamKind = ExamKind.Homework,
            IsAssignedToMe = true,
            ScorePublished = false
        },
        new ExamScheduleViewModel
        {
            Id = 3,
            Title = "Thi thử cuối tháng — Hóa vô cơ",
            Description = "Đợt kiểm tra định kỳ cuối tháng của trung tâm, tổng hợp hóa vô cơ, khởi tạo từ ngân hàng câu hỏi theo cấu trúc đề thi THPT.",
            Duration = 50,
            StartTime = DateTime.Now.AddDays(3),
            EndTime = DateTime.Now.AddDays(5),
            MaxStudents = null,
            SubjectName = "Hóa học",
            QuestionCount = 40,
            HasRegistered = false,
            CanRegister = true,
            ExamKind = ExamKind.MonthlyMock,
            IsAssignedToMe = true,
            ScorePublished = true
        },
        new ExamScheduleViewModel
        {
            Id = 4,
            Title = "Thi thử THPT Quốc gia miễn phí — Đợt 3 (Toán - Lý - Hóa)",
            Description = "Đợt thi thử mở công khai toàn quốc dành cho học sinh mọi trường, mô phỏng đúng cấu trúc đề thi tốt nghiệp, kết quả hiển thị ngay sau khi nộp bài.",
            Duration = 90,
            StartTime = DateTime.Now.AddDays(5),
            EndTime = DateTime.Now.AddDays(8),
            MaxStudents = 500,
            SubjectName = "Tự nhiên",
            QuestionCount = 75,
            HasRegistered = false,
            CanRegister = true,
            ExamKind = ExamKind.PublicMock,
            IsAssignedToMe = false,
            ScorePublished = true
        },
        new ExamScheduleViewModel
        {
            Id = 5,
            Title = "Thi thử cuối tháng — Di truyền học",
            Description = "Kiểm tra định kỳ bộ môn Sinh học lớp 12, phạm vi chương cơ chế di truyền và biến dị.",
            Duration = 50,
            StartTime = DateTime.Now.AddDays(-5),
            EndTime = DateTime.Now.AddDays(-2),
            MaxStudents = 180,
            SubjectName = "Sinh học",
            QuestionCount = 40,
            HasRegistered = true,
            CanRegister = false,
            ExamKind = ExamKind.MonthlyMock,
            IsAssignedToMe = true,
            ScorePublished = true
        },
        new ExamScheduleViewModel
        {
            Id = 6,
            Title = "Thi thử THPT Quốc gia miễn phí — Đợt 2 (Toán - Văn - Anh)",
            Description = "Đợt thi thử công khai bám sát đề minh họa của Bộ Giáo dục, đăng ký mở cho mọi học sinh lớp 12 vãng lai.",
            Duration = 90,
            StartTime = DateTime.Now.AddDays(7),
            EndTime = DateTime.Now.AddDays(10),
            MaxStudents = 500,
            SubjectName = "Xã hội",
            QuestionCount = 75,
            HasRegistered = false,
            CanRegister = true,
            ExamKind = ExamKind.PublicMock,
            IsAssignedToMe = false,
            ScorePublished = true
        },
        new ExamScheduleViewModel
        {
            Id = 7,
            Title = "Bài kiểm tra 15 phút — Ngữ pháp & Từ vựng",
            Description = "Kiểm tra kiến thức ngữ pháp trọng điểm tuần này của lớp Anh, gồm 10 câu trắc nghiệm trong 15 phút.",
            Duration = 15,
            StartTime = DateTime.Now.AddDays(-1),
            EndTime = DateTime.Now.AddDays(-1).AddHours(3),
            MaxStudents = 300,
            SubjectName = "Tiếng Anh",
            QuestionCount = 10,
            HasRegistered = true,
            CanRegister = false,
            ExamKind = ExamKind.QuickQuiz,
            IsAssignedToMe = true,
            ScorePublished = true
        },
        new ExamScheduleViewModel
        {
            Id = 8,
            Title = "Bài tập về nhà — Lịch sử Việt Nam 1919-1975",
            Description = "20 câu trắc nghiệm ôn tập lịch sử Việt Nam giai đoạn 1919 - 1975, nộp trước buổi học thứ Năm.",
            Duration = 30,
            StartTime = DateTime.Now.AddDays(1),
            EndTime = DateTime.Now.AddDays(2),
            MaxStudents = 120,
            SubjectName = "Lịch sử",
            QuestionCount = 20,
            HasRegistered = false,
            CanRegister = true,
            ExamKind = ExamKind.Homework,
            IsAssignedToMe = true,
            ScorePublished = false
        },
        new ExamScheduleViewModel
        {
            Id = 9,
            Title = "Thi thử THPT Quốc gia miễn phí — Đợt 4 tổng hợp",
            Description = "Đợt thi thử tổng hợp toàn bộ khối tự nhiên, độ khó tương đương đề thi chính thức, chấm điểm và phân tích chi tiết từng câu.",
            Duration = 90,
            StartTime = DateTime.Now.AddDays(10),
            EndTime = DateTime.Now.AddDays(13),
            MaxStudents = null,
            SubjectName = "Tự nhiên",
            QuestionCount = 75,
            HasRegistered = false,
            CanRegister = true,
            ExamKind = ExamKind.PublicMock,
            IsAssignedToMe = false,
            ScorePublished = true
        }
    };

    public static List<ExamResultViewModel> CompletedExams = new()
    {
        new ExamResultViewModel
        {
            Id = 1,
            ExamId = 1,
            ExamTitle = "Kiểm tra 15 phút — Hàm số",
            Score = 9.0m,
            TotalQuestions = 10,
            CorrectAnswers = 9,
            WrongAnswers = 1,
            StartTime = DateTime.Now.AddDays(-1),
            EndTime = DateTime.Now.AddDays(-1).AddMinutes(13),
            DurationUsed = 13
        },
        new ExamResultViewModel
        {
            Id = 2,
            ExamId = 2,
            ExamTitle = "Bài tập về nhà — Điện xoay chiều",
            Score = 8.0m,
            TotalQuestions = 10,
            CorrectAnswers = 8,
            WrongAnswers = 2,
            StartTime = DateTime.Now.AddDays(-1),
            EndTime = DateTime.Now.AddDays(-1).AddMinutes(28),
            DurationUsed = 28
        },
        new ExamResultViewModel
        {
            Id = 5,
            ExamId = 5,
            ExamTitle = "Thi thử cuối tháng — Di truyền học",
            Score = 9.0m,
            TotalQuestions = 40,
            CorrectAnswers = 36,
            WrongAnswers = 4,
            StartTime = DateTime.Now.AddDays(-5),
            EndTime = DateTime.Now.AddDays(-5).AddMinutes(42),
            DurationUsed = 42
        },
        new ExamResultViewModel
        {
            Id = 7,
            ExamId = 7,
            ExamTitle = "Bài kiểm tra 15 phút — Ngữ pháp & Từ vựng",
            Score = 7.8m,
            TotalQuestions = 10,
            CorrectAnswers = 8,
            WrongAnswers = 2,
            StartTime = DateTime.Now.AddDays(-1).AddHours(-2),
            EndTime = DateTime.Now.AddDays(-1).AddHours(-2).AddMinutes(14),
            DurationUsed = 14
        }
    };
    public static List<LearningItemViewModel> GetLearningSchedule()
    {
        var exam1 = Exams.First(e => e.Id == 1);
        var items = new List<LearningItemViewModel>
        {
            new()
            {
                Id = 1,
                Title = "Bài tập về nhà — Điện xoay chiều",
                SubjectName = "Vật lý",
                Type = "Bài tập về nhà",
                Description = "10 câu trắc nghiệm điện xoay chiều",
                DueTime = DateTime.Now.AddHours(5),
                IsDone = false,
                ExamId = 2,
                StatusText = "Sắp đến hạn"
            },
            new()
            {
                Id = 2,
                Title = "Bài kiểm tra 15 phút — Hàm số",
                SubjectName = "Toán",
                Type = "Kiểm tra 15 phút",
                Description = "10 câu trắc nghiệm hàm số",
                DueTime = DateTime.Now.AddDays(2),
                IsDone = true,
                ExamId = 1,
                StatusText = "Đã hoàn thành"
            },
            new()
            {
                Id = 3,
                Title = "Kiểm tra 15 phút — Điện xoay chiều (bù)",
                SubjectName = "Vật lý",
                Type = "Kiểm tra 15 phút",
                Description = "Buổi kiểm tra bù cho học viên vắng buổi trước",
                DueTime = DateTime.Now.AddHours(-3),
                IsDone = false,
                ExamId = 8,
                StatusText = "Quá hạn"
            },
            new()
            {
                Id = 4,
                Title = "Thi thử cuối tháng — Hóa vô cơ",
                SubjectName = "Hóa học",
                Type = "Thi thử cuối tháng",
                Description = "Đợt kiểm tra định kỳ cuối tháng",
                DueTime = DateTime.Now.AddDays(4),
                IsDone = false,
                ExamId = 3,
                StatusText = "Sắp đến hạn"
            },
            new()
            {
                Id = 5,
                Title = "Bài tập về nhà — Lịch sử Việt Nam 1919-1975",
                SubjectName = "Lịch sử",
                Type = "Bài tập về nhà",
                Description = "20 câu trắc nghiệm ôn tập",
                DueTime = DateTime.Now.AddDays(2),
                IsDone = false,
                ExamId = 8,
                StatusText = "Sắp đến hạn"
            },
            new()
            {
                Id = 6,
                Title = "Thi thử cuối tháng — Di truyền học",
                SubjectName = "Sinh học",
                Type = "Thi thử cuối tháng",
                Description = "Đợt thi thử đã hoàn thành",
                DueTime = DateTime.Now.AddDays(-5),
                IsDone = true,
                ExamId = 5,
                StatusText = "Đã hoàn thành"
            }
        };
        return items;
    }

    public static List<QuestionViewModel> GetQuestionsForExam(int examId)
    {
        var (subject, count, contents) = examId switch
        {
            1 => ("Toán", 10, new[] { "Cho hàm số y = x³ - 3x + 2. Hàm số đạt cực đại tại điểm có hoành độ bằng:", "Tập xác định của hàm số y = (2x - 1)/(x + 3) là:", "Đường tiệm cận ngang của đồ thị hàm số y = (3x - 1)/(x + 2) là:", "Hàm số y = x⁴ - 2x² + 1 đồng biến trên khoảng nào?", "Cho hàm số y = f(x) liên tục trên R. Tìm mệnh đề đúng:" }),
            2 => ("Vật lý", 10, new[] { "Trong mạch điện xoay chiều RLC mắc nối tiếp, khi xảy ra cộng hưởng điện thì:", "Công suất tiêu thụ của mạch điện xoay chiều RLC được tính bằng công thức nào?", "Hệ số công suất của đoạn mạch xoay chiều là:", "Một máy biến áp lý tưởng có cuộn sơ cấp 500 vòng, cuộn thứ cấp 250 vòng. Nếu điện áp sơ cấp là 220V thì điện áp thứ cấp là:", "Tần số góc của dòng điện xoay chiều có chu kỳ T là:" }),
            3 => ("Hóa học", 40, new[] { "Dãy kim loại nào sau đây được xếp theo chiều tính kim loại tăng dần?", "Phản ứng đặc trưng để nhận biết ion Fe³⁺ trong dung dịch là:", "Chất nào sau đây là oxit axit?", "Nước cứng tạm thời chứa các ion nào sau đây?", "Số oxi hóa của lưu huỳnh (S) trong H₂SO₄ là:" }),
            4 => ("Tự nhiên", 75, new[] { "Đề thi thử THPT Quốc gia đợt 3 — phần Toán: giới hạn, đạo hàm, hàm số mũ - logarit:", "Phần Vật lý: dao động điều hòa, sóng cơ, dòng điện xoay chiều:", "Phần Hóa học: este - lipit, cacbohidrat, amin - amino axit:", "Phần Toán: hình học không gian, Oxyz, số phức:", "Phần Vật lý: lượng tử ánh sáng, hạt nhân nguyên tử:" }),
            5 => ("Sinh học", 40, new[] { "Đơn phân của ADN là:", "Hiện tượng nào sau đây là đột biến gen?", "Quy luật phân li độc lập của Menđen được phát biểu như thế nào?", "Kiểu hình là gì?", "Loại ARN nào mang bộ ba đối mã (anticodon)?" }),
            6 => ("Xã hội", 75, new[] { "Đề thi thử THPT Quốc gia đợt 2 — phần Ngữ văn: đọc hiểu và nghị luận:", "Phần Tiếng Anh: ngữ âm, từ vựng, ngữ pháp, đọc hiểu:", "Phần Toán: số phức, tích phân, không gian Oxyz:", "Phần Tiếng Anh: viết lại câu, tìm lỗi sai:", "Phần Ngữ văn: nghị luận xã hội về hiện tượng đời sống:" }),
            7 => ("Tiếng Anh", 10, new[] { "Choose the word with a different stress pattern:", "The students ______ in the library now.", "Choose the correct answer: If I had known, I ______ to the party.", "Which word is the synonym of \"generous\"?", "Reported speech: She said she ______ the following day." }),
            8 => ("Lịch sử", 20, new[] { "Phong trào cách mạng Việt Nam giai đoạn 1930 - 1931 gắn liền với sự kiện nào?", "Chiến dịch Điện Biên Phủ diễn ra vào năm nào?", "Nhiệm vụ của cách mạng miền Nam sau Hiệp định Giơnevơ 1954 là:", "Sự kiện nào đánh dấu bước ngoặt của cách mạng Việt Nam năm 1945?", "Phong trào \"Đồng khởi\" bùng nổ ở đâu đầu tiên?" }),
            _ => ("Tự nhiên", 75, new[] { "Đề thi thử THPT Quốc gia đợt 4 tổng hợp — phần Toán:", "Phần Vật lý:", "Phần Hóa học:", "Phần Sinh học:", "Phần Toán — vận dụng cao:" })
        };

        var questions = new List<QuestionViewModel>();
        for (int i = 0; i < count; i++)
        {
            var content = i < contents.Length ? contents[i] : $"Nội dung câu hỏi {i + 1} môn {subject} — được trộn ngẫu nhiên từ ngân hàng câu hỏi.";
            questions.Add(BuildQuestion(i + 1, content));
        }
        return questions;
    }

    private static QuestionViewModel BuildQuestion(int order, string content)
    {
        var letters = new[] { "A", "B", "C", "D" };
        return new QuestionViewModel
        {
            Id = 1000 + order,
            OrderIndex = order,
            Content = content,
            TypeName = "Trắc nghiệm một đáp án",
            Options = letters.Select((letter, idx) => new OptionViewModel
            {
                Id = 100 + order * 10 + idx,
                Content = $"Phương án lựa chọn {letter} — nội dung chi tiết cho câu hỏi số {order}.",
                Label = letter,
                IsSelected = false
            }).ToList()
        };
    }

    public static ExamResultViewModel BuildResultFromQuestions(int examId, List<QuestionViewModel> questions)
    {
        var exam = Exams.FirstOrDefault(e => e.Id == examId);
        var result = CompletedExams.FirstOrDefault(r => r.ExamId == examId)
                     ?? CompletedExams.FirstOrDefault();

        var questionResults = questions.Select((q, idx) => new QuestionResultViewModel
        {
            OrderIndex = idx + 1,
            Content = q.Content,
            Options = q.Options.Select(o =>
            {
                var isCorrect = idx % 4 == 0 && o.Label == "B";
                var isSelected = o.Label == "B" || (!isCorrect && idx % 3 == 0 && o.Label == "C");
                return new OptionResultViewModel
                {
                    Id = o.Id,
                    Label = o.Label,
                    Content = o.Content,
                    IsCorrect = o.Label == "B",
                    IsSelected = isSelected
                };
            }).ToList(),
            SelectedOptionId = q.Options.First(o => o.Label == "B").Id,
            IsCorrect = true
        }).ToList();

        for (int i = 0; i < questionResults.Count; i += 5)
        {
            questionResults[i].IsCorrect = false;
            questionResults[i].Options.ForEach(o =>
            {
                o.IsCorrect = o.Label == "C";
                o.IsSelected = o.Label == "A";
            });
            questionResults[i].SelectedOptionId = 0;
        }

        var correct = questionResults.Count(q => q.IsCorrect);
        var total = questionResults.Count;
        var score = total > 0 ? Math.Round((decimal)correct / total * 10, 2) : 0m;

        return new ExamResultViewModel
        {
            Id = examId,
            ExamTitle = exam?.Title ?? $"{exam?.SubjectName} — Kỳ thi #{examId}",
            Score = score,
            TotalQuestions = total,
            CorrectAnswers = correct,
            WrongAnswers = total - correct,
            StartTime = DateTime.Now.AddMinutes(-DurationMinutes(exam?.Duration ?? 50)),
            EndTime = DateTime.Now,
            DurationUsed = exam?.Duration ?? 50,
            QuestionResults = questionResults
        };
    }

    private static int DurationMinutes(int duration) => duration;
}