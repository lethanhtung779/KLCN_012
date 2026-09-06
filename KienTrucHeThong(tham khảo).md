# KIẾN TRÚC HỆ THỐNG - XÂY DỰNG HỆ THỐNG QUẢN LÝ VÀ THI TRÁC NGHIỆM TRỰC TUYẾN

## 1. KIẾN TRÚC TỔNG THỂ (OVERALL ARCHITECTURE)

```
┌─────────────────────────────────────────────────────────────────────┐
│                        CLIENT LAYER                                 │
├─────────────────────────┬───────────────────────────────────────────┤
│   WinForms App (.NET)   │        Website (ASP.NET Core MVC)        │
│   (Admin + Teacher)     │              (Student)                    │
├─────────────────────────┴───────────────────────────────────────────┤
│                     API LAYER (REST API)                            │
│              ASP.NET Core Web API (Shared)                          │
├─────────────────────────────────────────────────────────────────────┤
│                     BUSINESS LOGIC LAYER                            │
│         Services / Repositories / Unit of Work                     │
├─────────────────────────────────────────────────────────────────────┤
│                     DATA ACCESS LAYER                               │
│            Entity Framework Core (Code First)                       │
├─────────────────────────────────────────────────────────────────────┤
│                     DATABASE                                        │
│                    SQL Server                                       │
└─────────────────────────────────────────────────────────────────────┘
```

### Kiến trúc 3-tier / N-layer:
- **Presentation Layer:** WinForms + Website (2 UI riêng biệt)
- **Business Logic Layer:** API + Services (chung cho cả 2 nền tảng)
- **Data Access Layer:** EF Core + SQL Server (chung)

---

## 2. SOLUTION STRUCTURE

```
QLThiTN/
├── QLThiTN.API/                    # REST API (ASP.NET Core Web API)
│   ├── Controllers/                # API Controllers
│   ├── Services/                   # Business Logic Services
│   ├── Repositories/               # Data Access
│   ├── Models/                     # Entity Models
│   ├── DTOs/                       # Data Transfer Objects
│   ├── Mappings/                   # AutoMapper Profiles
│   ├── Middleware/                  # Auth, Error Handling
│   ├── Filters/                    # Action Filters
│   └── Program.cs
│
├── QLThiTN.Web/                    # Website - Thí sinh (ASP.NET Core MVC)
│   ├── Controllers/                # MVC Controllers
│   ├── Views/                      # Razor Views
│   ├── ViewModels/                 # View Models
│   ├── wwwroot/                    # Static files (CSS, JS, Images)
│   ├── Services/                   # API Client Services
│   └── Program.cs
│
├── QLThiTN.WinForms/              # WinForms - Admin & Giáo viên
│   ├── Forms/                      # Main Forms
│   │   ├── Auth/                   # Login, Register
│   │   ├── Admin/                  # Admin Forms
│   │   ├── Teacher/                # Teacher Forms
│   │   └── Common/                 # Shared Forms
│   ├── Controls/                   # Custom User Controls
│   ├── Services/                   # API Client Services
│   ├── Models/                     # View Models
│   └── Program.cs
│
├── QLThiTN.Core/                   # Shared Library (Class Library)
│   ├── Entities/                   # Domain Entities
│   ├── Enums/                      # Enumerations
│   ├── Constants/                  # App Constants
│   ├── Interfaces/                 # Service Interfaces
│   └── Helpers/                    # Utility Classes
│
└── QLThiTN.sln
```

---

## 3. PHÂN CHIA MODULE CHỨC NĂNG

### 3.1. MODULE CHUNG (Shared Modules)

| Module | Mô tả |
|--------|-------|
| **Auth** | Đăng nhập, đăng xuất, phân quyền, JWT Token |
| **User** | Quản lý tài khoản, Roles (Admin/Teacher/Student) |
| **Notification** | Thông báo hệ thống |
| **Backup** | Sao lưu & phục hồi dữ liệu |

---

### 3.2. MODULE WINFORMS (Admin + Giáo viên)

#### 3.2.1. Module Quản trị viên (Admin)

| Module | Chức năng | Form |
|--------|-----------|------|
| **Quản lý tài khoản** | Thêm/Sửa/Xóa/Khóa/Mở tài khoản GV & TS | `frmUserManagement` |
| **Phân quyền** | Gán quyền cho người dùng | `frmRoleAssignment` |
| **Ngân hàng câu hỏi** | Tạo/Sửa/Xóa câu hỏi, phân loại theo môn/chủ đề/độ khó | `frmQuestionBank` |
| **Quản lý môn học** | CRUD môn học, chủ đề | `frmSubject` |
| **Tạo đề thi** | Tạo đề thi tự động/thủ công từ ngân hàng câu hỏi | `frmExamBuilder` |
| **Tạo kỳ thi** | Thiết lập thời gian, giới hạn truy cập, số lượng TS | `frmExamSchedule` |
| **Thống kê kết quả** | Theo kỳ thi, theo thí sinh, theo câu hỏi | `frmStatistics` |
| **Báo cáo** | Xuất báo cáo Excel/PDF | `frmReport` |
| **Backup/Restore** | Sao lưu & khôi phục CSDL | `frmBackup` |
| **Log hệ thống** | Xem nhật ký hoạt động | `frmSystemLog` |

#### 3.2.2. Module Giáo viên

| Module | Chức năng | Form |
|--------|-----------|------|
| **Ngân hàng câu hỏi** | Nhập/Sửa/Xóa câu hỏi, import từ Excel/TXT | `frmQuestionBank` |
| **Tạo đề thi** | Trộn đề, trộn câu hỏi, trộn đáp án | `frmExamBuilder` |
| **Xem kết quả thi** | Theo lớp, môn học | `frmExamResults` |
| **Thống kê chất lượng** | Tỷ lệ đúng/sai theo câu hỏi | `frmQuestionStats` |

---

### 3.3. MODULE WEBSITE (Thí sinh)

| Module | Chức năng | View |
|--------|-----------|------|
| **Tài khoản** | Đăng ký, đăng nhập, quản lý hồ sơ | `Login.cshtml`, `Register.cshtml`, `Profile.cshtml` |
| **Kỳ thi** | Xem lịch thi, thông tin kỳ thi | `ExamSchedule.cshtml`, `ExamDetail.cshtml` |
| **Làm bài thi** | Truy cập đề thi, chọn đáp án, đồng hồ đếm ngược | `TakingExam.cshtml` |
| **Kết quả** | Xem điểm, lịch sử thi, chi tiết bài thi | `ExamResult.cshtml`, `ExamHistory.cshtml` |

---

## 4. SƠ ĐỒ ERD (ENTITY RELATIONSHIP DIAGRAM)

```
┌──────────────┐       ┌──────────────┐       ┌──────────────┐
│    Users      │       │   Subjects   │       │  Questions   │
├──────────────┤       ├──────────────┤       ├──────────────┤
│ Id (PK)      │       │ Id (PK)      │       │ Id (PK)      │
│ Username     │       │ Name         │       │ Content      │
│ Password     │       │ Code         │       │ Type (enum)  │
│ FullName     │       │ Description  │       │ SubjectId(FK)│
│ Email        │       └──────────────┘       │ TopicId (FK) │
│ Phone        │              │               │ Difficulty   │
│ Role (enum)  │              │               │ CreatedBy(FK)│
│ IsActive     │              │               │ IsActive     │
│ CreatedAt    │              │               │ CreatedAt    │
│ UpdatedAt    │              │               └──────────────┘
└──────────────┘              │                      │
       │                      │               ┌──────────────┐
       │                      │               │  Options     │
       │                      │               ├──────────────┤
       │                      │               │ Id (PK)      │
       │                      │               │ QuestionId(FK│
       │                      │               │ Content      │
       │                      │               │ IsCorrect    │
       │                      │               │ OrderIndex   │
       │                      │               └──────────────┘
       │                      │
       │               ┌──────────────┐       ┌──────────────┐
       │               │   Topics     │       │    Exams     │
       │               ├──────────────┤       ├──────────────┤
       │               │ Id (PK)      │       │ Id (PK)      │
       │               │ Name         │       │ Title        │
       │               │ SubjectId(FK)│       │ Description  │
       │               └──────────────┘       │ Duration     │
       │                                      │ StartTime    │
       │                                      │ EndTime      │
       │                                      │ MaxStudents  │
       │                                      │ IsActive     │
       │                                      │ CreatedBy(FK)│
       │                                      └──────────────┘
       │                                              │
       │                                     ┌──────────────┐
       │                                     │ ExamQuestions │
       │                                     ├──────────────┤
       │                                     │ Id (PK)      │
       │                                     │ ExamId (FK)  │
       │                                     │ QuestionId(FK│
       │                                     │ OrderIndex   │
       │                                     └──────────────┘
       │
       │                                     ┌──────────────┐
       │                                     │  ExamResults  │
       │                                     ├──────────────┤
       │                                     │ Id (PK)      │
       │                                     │ ExamId (FK)  │
       │                                     │ StudentId(FK)│
       │                                     │ Score        │
       │                                     │ StartTime    │
       │                                     │ EndTime      │
       │                                     │ Status       │
       │                                     └──────────────┘
       │                                              │
       │                                     ┌──────────────┐
       │                                     │ResultDetails │
       │                                     ├──────────────┤
       │                                     │ Id (PK)      │
       │                                     │ ResultId(FK) │
       │                                     │ QuestionId(FK│
       │                                     │ SelectedOptId│
       │                                     │ IsCorrect    │
       │                                     └──────────────┘
       │
       │                                     ┌──────────────┐
       └────────────────────────────────────>│  ExamLogs     │
                                             ├──────────────┤
                                             │ Id (PK)      │
                                             │ StudentId(FK)│
                                             │ ExamId (FK)  │
                                             │ Action       │
                                             │ Timestamp    │
                                             │ IP Address   │
                                             └──────────────┘
```

---

## 5. DATABASE TABLES

### 5.1. Bảng Users
```sql
CREATE TABLE Users (
    Id          INT PRIMARY KEY IDENTITY,
    Username    NVARCHAR(50) UNIQUE NOT NULL,
    Password    NVARCHAR(256) NOT NULL,  -- Hashed
    FullName    NVARCHAR(100) NOT NULL,
    Email       NVARCHAR(100),
    Phone       NVARCHAR(20),
    Role        INT NOT NULL,            -- 0: Admin, 1: Teacher, 2: Student
    IsActive    BIT DEFAULT 1,
    CreatedAt   DATETIME DEFAULT GETDATE(),
    UpdatedAt   DATETIME
);
```

### 5.2. Bảng Subjects
```sql
CREATE TABLE Subjects (
    Id          INT PRIMARY KEY IDENTITY,
    Name        NVARCHAR(100) NOT NULL,
    Code        NVARCHAR(20) UNIQUE NOT NULL,
    Description NVARCHAR(500)
);
```

### 5.3. Bảng Topics
```sql
CREATE TABLE Topics (
    Id          INT PRIMARY KEY IDENTITY,
    Name        NVARCHAR(100) NOT NULL,
    SubjectId   INT FOREIGN KEY REFERENCES Subjects(Id)
);
```

### 5.4. Bảng Questions
```sql
CREATE TABLE Questions (
    Id          INT PRIMARY KEY IDENTITY,
    Content     NVARCHAR(MAX) NOT NULL,
    Type        INT NOT NULL,            -- 0: SingleChoice, 1: MultipleChoice, 2: TrueFalse
    SubjectId   INT FOREIGN KEY REFERENCES Subjects(Id),
    TopicId     INT FOREIGN KEY REFERENCES Topics(Id),
    Difficulty  INT NOT NULL,            -- 0: Easy, 1: Medium, 2: Hard
    CreatedBy   INT FOREIGN KEY REFERENCES Users(Id),
    IsActive    BIT DEFAULT 1,
    CreatedAt   DATETIME DEFAULT GETDATE()
);
```

### 5.5. Bảng Options
```sql
CREATE TABLE Options (
    Id          INT PRIMARY KEY IDENTITY,
    QuestionId  INT FOREIGN KEY REFERENCES Questions(Id),
    Content     NVARCHAR(500) NOT NULL,
    IsCorrect   BIT DEFAULT 0,
    OrderIndex  INT DEFAULT 0
);
```

### 5.6. Bảng Exams
```sql
CREATE TABLE Exams (
    Id          INT PRIMARY KEY IDENTITY,
    Title       NVARCHAR(200) NOT NULL,
    Description NVARCHAR(500),
    Duration    INT NOT NULL,            -- phút
    StartTime   DATETIME,
    EndTime     DATETIME,
    MaxStudents INT,
    IsActive    BIT DEFAULT 1,
    CreatedBy   INT FOREIGN KEY REFERENCES Users(Id),
    CreatedAt   DATETIME DEFAULT GETDATE()
);
```

### 5.7. Bảng ExamQuestions
```sql
CREATE TABLE ExamQuestions (
    Id          INT PRIMARY KEY IDENTITY,
    ExamId      INT FOREIGN KEY REFERENCES Exams(Id),
    QuestionId  INT FOREIGN KEY REFERENCES Questions(Id),
    OrderIndex  INT DEFAULT 0
);
```

### 5.8. Bảng ExamResults
```sql
CREATE TABLE ExamResults (
    Id          INT PRIMARY KEY IDENTITY,
    ExamId      INT FOREIGN KEY REFERENCES Exams(Id),
    StudentId   INT FOREIGN KEY REFERENCES Users(Id),
    Score       DECIMAL(5,2),
    StartTime   DATETIME,
    EndTime     DATETIME,
    Status      INT NOT NULL,            -- 0: InProgress, 1: Completed, 2: Submitted
    CreatedAt   DATETIME DEFAULT GETDATE()
);
```

### 5.9. Bảng ResultDetails
```sql
CREATE TABLE ResultDetails (
    Id              INT PRIMARY KEY IDENTITY,
    ResultId        INT FOREIGN KEY REFERENCES ExamResults(Id),
    QuestionId      INT FOREIGN KEY REFERENCES Questions(Id),
    SelectedOptionId INT FOREIGN KEY REFERENCES Options(Id),
    IsCorrect       BIT DEFAULT 0
);
```

### 5.10. Bảng ExamLogs
```sql
CREATE TABLE ExamLogs (
    Id          INT PRIMARY KEY IDENTITY,
    StudentId   INT FOREIGN KEY REFERENCES Users(Id),
    ExamId      INT FOREIGN KEY REFERENCES Exams(Id),
    Action      NVARCHAR(100),
    Timestamp   DATETIME DEFAULT GETDATE(),
    IPAddress   NVARCHAR(50)
);
```

---

## 6. ENUM & CONSTANTS

```csharp
// Roles
public enum UserRole
{
    Admin = 0,
    Teacher = 1,
    Student = 2
}

// Question Types
public enum QuestionType
{
    SingleChoice = 0,
    MultipleChoice = 1,
    TrueFalse = 2
}

// Difficulty
public enum DifficultyLevel
{
    Easy = 0,
    Medium = 1,
    Hard = 2
}

// Exam Status
public enum ExamStatus
{
    InProgress = 0,
    Completed = 1,
    Submitted = 2
}
```

---

## 7. API ENDPOINTS

### 7.1. Auth
| Method | Endpoint | Mô tả |
|--------|----------|-------|
| POST | `/api/auth/login` | Đăng nhập |
| POST | `/api/auth/logout` | Đăng xuất |
| POST | `/api/auth/refresh` | Refresh Token |

### 7.2. Users
| Method | Endpoint | Mô tả |
|--------|----------|-------|
| GET | `/api/users` | Danh sách người dùng |
| GET | `/api/users/{id}` | Chi tiết người dùng |
| POST | `/api/users` | Tạo người dùng |
| PUT | `/api/users/{id}` | Cập nhật người dùng |
| DELETE | `/api/users/{id}` | Xóa người dùng |
| PUT | `/api/users/{id}/lock` | Khóa/Mở tài khoản |

### 7.3. Questions
| Method | Endpoint | Mô tả |
|--------|----------|-------|
| GET | `/api/questions` | Danh sách câu hỏi (filter) |
| GET | `/api/questions/{id}` | Chi tiết câu hỏi |
| POST | `/api/questions` | Tạo câu hỏi |
| PUT | `/api/questions/{id}` | Cập nhật câu hỏi |
| DELETE | `/api/questions/{id}` | Xóa câu hỏi |
| POST | `/api/questions/import` | Import từ Excel/TXT |

### 7.4. Exams
| Method | Endpoint | Mô tả |
|--------|----------|-------|
| GET | `/api/exams` | Danh sách đề thi |
| GET | `/api/exams/{id}` | Chi tiết đề thi |
| POST | `/api/exams` | Tạo đề thi |
| PUT | `/api/exams/{id}` | Cập nhật đề thi |
| DELETE | `/api/exams/{id}` | Xóa đề thi |
| POST | `/api/exams/{id}/questions` | Thêm câu hỏi vào đề |
| POST | `/api/exams/{id}/generate` | Tạo đề thi tự động |

### 7.5. Exam Results
| Method | Endpoint | Mô tả |
|--------|----------|-------|
| GET | `/api/results` | Danh sách kết quả |
| GET | `/api/results/{id}` | Chi tiết kết quả |
| POST | `/api/results` | Nộp bài thi |
| GET | `/api/results/statistics` | Thống kê kết quả |

### 7.6. Subjects & Topics
| Method | Endpoint | Mô tả |
|--------|----------|-------|
| GET | `/api/subjects` | Danh sách môn học |
| POST | `/api/subjects` | Tạo môn học |
| GET | `/api/topics` | Danh sách chủ đề |
| POST | `/api/topics` | Tạo chủ đề |

---

## 8. FLOW DỮ LIỆU

### 8.1. Flow Làm Bài Thi (Website → API → DB)

```
Student (Website)                    API                        Database
      │                              │                              │
      │  1. GET /api/exams/{id}     │                              │
      │─────────────────────────────>│                              │
      │                              │  2. Query Exam + Questions   │
      │                              │─────────────────────────────>│
      │  3. Return Exam Data         │                              │
      │<─────────────────────────────│                              │
      │                              │                              │
      │  [Student takes exam]        │                              │
      │                              │                              │
      │  4. POST /api/results       │                              │
      │     {ExamId, Answers[]}     │                              │
      │─────────────────────────────>│                              │
      │                              │  5. Save Result              │
      │                              │─────────────────────────────>│
      │                              │  6. Calculate Score          │
      │  7. Return Score             │                              │
      │<─────────────────────────────│                              │
```

### 8.2. Flow Đồng Bộ (WinForms → API → DB)

```
Admin/Teacher (WinForms)           API                        Database
      │                              │                              │
      │  1. POST /api/questions     │                              │
      │     (Create Question)       │                              │
      │─────────────────────────────>│                              │
      │                              │  2. Save to DB               │
      │                              │─────────────────────────────>│
      │  3. Return Success          │                              │
      │<─────────────────────────────│                              │
      │                              │                              │
      │  [Website reads same data]  │                              │
      │                              │                              │
```

---

## 9. BẢNG PHÂN QUYền

| Chức năng | Admin | Teacher | Student |
|-----------|:-----:|:-------:|:-------:|
| Quản lý tài khoản | ✅ | ❌ | ❌ |
| Quản lý môn học | ✅ | ❌ | ❌ |
| Ngân hàng câu hỏi | ✅ | ✅ (môn mình) | ❌ |
| Tạo đề thi | ✅ | ✅ | ❌ |
| Tạo kỳ thi | ✅ | ❌ | ❌ |
| Import câu hỏi | ✅ | ✅ | ❌ |
| Xem kết quả (tất cả) | ✅ | ✅ (lớp mình) | ❌ |
| Thống kê | ✅ | ✅ | ❌ |
| Backup/Restore | ✅ | ❌ | ❌ |
| Đăng ký thi | ❌ | ❌ | ✅ |
| Làm bài thi | ❌ | ❌ | ✅ |
| Xem điểm cá nhân | ❌ | ❌ | ✅ |
| Quản lý hồ sơ | ✅ | ✅ | ✅ |

---

## 10. CÔNG NGHỆ SỬ DỤNG

| Layer | Technology |
|-------|------------|
| WinForms | C# (.NET 8 / .NET Framework 4.8) |
| Website | ASP.NET Core 8 MVC |
| API | ASP.NET Core 8 Web API |
| ORM | Entity Framework Core 8 |
| Auth | JWT Bearer + Refresh Token |
| DB | SQL Server 2019+ |
| Mapping | AutoMapper |
| UI WinForms | DevExpress / MaterialSkin |
| UI Website | Bootstrap 5 + jQuery |
| Charts | Chart.js / ECharts |
| Export | EPPlus (Excel), iTextSharp (PDF) |
| Logging | Serilog |
