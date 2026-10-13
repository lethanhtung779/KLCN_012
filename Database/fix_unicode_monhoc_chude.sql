-- Bổ sung dấu tiếng Việt, giữ nguyên ID và các liên kết hiện có.

USE [ThiTracNghiem];

SET XACT_ABORT ON;

BEGIN TRANSACTION;

UPDATE t SET TenMon = names.TenMoi
FROM dbo.MonHoc AS t
JOIN (VALUES
    (N'Toan', N'Toán'),
    (N'Vat Ly', N'Vật lý'),
    (N'Hoa Hoc', N'Hóa học'),
    (N'Sinh Hoc', N'Sinh học'),
    (N'Lich Su', N'Lịch sử'),
    (N'Dia Ly', N'Địa lý'),
    (N'Tin Hoc', N'Tin học'),
    (N'Kinh Te Phap Luat', N'Kinh tế pháp luật'),
    (N'Cong Nghe', N'Công nghệ'),
    (N'Tieng Anh', N'Tiếng Anh')
) AS names(TenCu, TenMoi) ON t.TenMon = names.TenCu;

UPDATE t SET TenChuDe = names.TenMoi
FROM dbo.ChuDe AS t
JOIN (VALUES
    (N'Cap so va Day so', N'Cấp số và Dãy số'),
    (N'Ham so va Dao ham', N'Hàm số và Đạo hàm'),
    (N'Hinh hoc Oxyz', N'Hình học Oxyz'),
    (N'Hinh hoc phang', N'Hình học phẳng'),
    (N'Luong giac', N'Lượng giác'),
    (N'Mu va Logarit', N'Mũ và Logarit'),
    (N'Nguyen ham va Tich phan', N'Nguyên hàm và Tích phân'),
    (N'Xac suat va Thong ke', N'Xác suất và Thống kê'),
    (N'Dao dong va Song', N'Dao động và Sóng'),
    (N'Dien hoc va Tu truong', N'Điện học và Từ trường'),
    (N'Hat nhan', N'Hạt nhân'),
    (N'Nhiet hoc', N'Nhiệt học'),
    (N'Huu co', N'Hữu cơ'),
    (N'Nhan biet va Tach chat', N'Nhận biết và Tách chất'),
    (N'Pin Dien phan An mon', N'Pin Điện phân Ăn mòn'),
    (N'Toc do va Can bang', N'Tốc độ và Cân bằng'),
    (N'Vo co', N'Vô cơ'),
    (N'Co the nguoi', N'Cơ thể người'),
    (N'Di truyen va ADN', N'Di truyền và ADN'),
    (N'Enzim va Trao doi chat', N'Enzim và Trao đổi chất'),
    (N'Sinh thai va Moi truong', N'Sinh thái và Môi trường'),
    (N'Dang va Cach mang', N'Đảng và Cách mạng'),
    (N'Doi moi va Hoi nhap', N'Đổi mới và Hội nhập'),
    (N'Khang chien va Thong nhat', N'Kháng chiến và Thống nhất'),
    (N'The gioi va Trat tu', N'Thế giới và Trật tự'),
    (N'Kinh te va Xa hoi', N'Kinh tế và Xã hội'),
    (N'Tu nhien', N'Tự nhiên'),
    (N'Vung va Do thi', N'Vùng và Đô thị'),
    (N'An toan thong tin', N'An toàn thông tin'),
    (N'Bang tinh va CSDL', N'Bảng tính và CSDL'),
    (N'Lap trinh va Thuat toan', N'Lập trình và Thuật toán'),
    (N'Mang Internet va Web', N'Mạng Internet và Web'),
    (N'Nhe CNTT va Ung dung', N'Nghề CNTT và Ứng dụng'),
    (N'Tri tue nhan tao', N'Trí tuệ nhân tạo'),
    (N'Kinh te vi mo', N'Kinh tế vi mô'),
    (N'Phap luat', N'Pháp luật'),
    (N'Cong nghe Cong nghiep', N'Công nghệ Công nghiệp'),
    (N'Doc hieu', N'Đọc hiểu'),
    (N'Giao tiep', N'Giao tiếp'),
    (N'Ngu am', N'Ngữ âm'),
    (N'Ngu phap va Tu vung', N'Ngữ pháp và Từ vựng')
) AS names(TenCu, TenMoi) ON t.TenChuDe = names.TenCu;

COMMIT TRANSACTION;
