# WorkHub – Website (SaaS) hỗ trợ giao tiếp, làm việc cộng tác cho doanh nghiệp

Bài tập lớn môn Lập trình Web nâng cao – ASP.NET Core MVC (.NET 8), Entity Framework Core, SQL Server, HTML/CSS/JS (AJAX), Bootstrap.

## Cách chạy

Yêu cầu: .NET 8 SDK và SQL Server LocalDB (cài kèm Visual Studio).

```bash
cd WorkHub
dotnet run --launch-profile http
```

Mở http://localhost:5041. Lần chạy đầu tiên chương trình tự tạo CSDL `WorkHubDb` trên `(localdb)\MSSQLLocalDB` và thêm dữ liệu mẫu (`Data/SeedData.cs`).
Muốn dùng SQL Server khác thì sửa `ConnectionStrings:DefaultConnection` trong `WorkHub/appsettings.json`.
Muốn làm lại dữ liệu mẫu từ đầu: xóa database `WorkHubDb` (SSMS hoặc `sqllocaldb`) rồi chạy lại.

## Tài khoản mẫu (mật khẩu đều là `123456`)

| Email | Vai trò |
|---|---|
| admin@workhub.vn | Super Admin (quản trị hệ thống) |
| khanh@abc.vn | TenantAdmin – Công ty ABC |
| huy@abc.vn | Member – trưởng Nhóm Web |
| hung@abc.vn | Member – trưởng Nhóm Kiểm thử |
| nam@xyz.vn | TenantAdmin – Công ty XYZ (dùng để thử cô lập dữ liệu) |

## Cấu trúc thư mục

```
WorkHub/
├── Program.cs                 Cấu hình: EF Core, Cookie Auth, CSRF, seed dữ liệu
├── Models/                    8 bảng: NguoiDung, DoanhNghiep, ThanhVienDoanhNghiep, NhomLamViec,
│   │                          ThanhVienNhom, CongViec, TraoDoiBinhLuan, ThongBao
│   └── ViewModels/            Biểu mẫu BM01–BM10 (có Data Annotations để validate)
├── Data/AppDbContext.cs       Quan hệ, ràng buộc, chỉ mục, Global Query Filter theo MaDoanhNghiep
├── Data/SeedData.cs           Dữ liệu mẫu
├── Services/
│   ├── CurrentUser.cs         Đọc mã người dùng / doanh nghiệp / vai trò từ Claims
│   ├── AuthService.cs         Băm mật khẩu, đăng nhập, kiểm tra tài khoản bị khóa mỗi request
│   ├── QuyenHelper.cs         Quy tắc "Member chỉ xem nhóm/công việc của mình"
│   └── NotificationService.cs Tạo thông báo
├── Controllers/               Home, Account, Tenant, Member, Group, Task, Comment, Notification, Admin
├── Views/                     Razor Views (+ Partial View cho các danh sách AJAX)
└── wwwroot/css/site.css       CSS + responsive; wwwroot/js/site.js: hàm AJAX dùng chung, chuông thông báo, chat
```

## Đối chiếu yêu cầu trong báo cáo

| Yêu cầu | Nơi thực hiện |
|---|---|
| R01–R03 Tạo, giao, sửa, xóa công việc | `TaskController` Create/Edit/Delete |
| R02 Cập nhật trạng thái (AJAX) | `POST /Task/UpdateStatus` – nút trên trang chi tiết và ô chọn trong danh sách |
| R04 Trao đổi trong nhóm (AJAX) | `CommentController` + khung chat ở `Group/Details` |
| R05 Bình luận công việc (AJAX) | `CommentController` + khung bình luận ở `Task/Details` |
| R06–R07 Tạo doanh nghiệp, cập nhật thông tin + logo | `TenantController` Create/Settings (logo JPG/PNG ≤ 2MB) |
| R08 Mời, đổi vai trò, vô hiệu hóa thành viên | `MemberController` |
| R09–R10 Tạo/sửa/đóng nhóm, thêm/loại thành viên (AJAX) | `GroupController` |
| R11–R12 Thông báo giao việc, đổi trạng thái, bình luận, nhắc tên `@Họ Tên` | `NotificationService`, chuông thông báo tự cập nhật 20 giây/lần |
| R13 Lọc công việc theo từ khóa, trạng thái, hạn, người phụ trách (AJAX) | `GET /Task/Search` |
| R14 Tìm thành viên, lọc nhóm (AJAX) | `GET /Member/Search`, `GET /Group/Index` |
| R15–R16 Đăng ký, đăng nhập, đăng xuất, hồ sơ, đổi mật khẩu | `AccountController` |
| R17–R18 Super Admin thống kê, khóa/mở doanh nghiệp và tài khoản | `AdminController` |
| R20 Responsive | Bootstrap grid + `@media` trong `site.css` (≥992px sidebar cố định, 768–991px thu gọn, <768px menu ẩn/hiện) |
| R21 Phân quyền + cô lập doanh nghiệp | `[Authorize(Roles=...)]`, Global Query Filter, `QuyenHelper`, kiểm tra quyền trong từng Action |
| R22 Validate 2 phía | Data Annotations + jQuery Validation (client) và `ModelState` (server) |
| R25 SEO | Trang chủ/Giới thiệu có `<title>`, `meta description`, h1–h3; trang nội bộ có `noindex` |

## An toàn thông tin đã áp dụng

- Mật khẩu băm bằng `PasswordHasher` (PBKDF2 có salt) của ASP.NET Core.
- Sai mật khẩu 5 lần thì tạm khóa 5 phút; thông báo lỗi chung, không tiết lộ email có tồn tại không.
- Cookie `HttpOnly`, `SameSite=Strict`, hết hạn sau 8 giờ (chạy HTTPS thì tự có cờ `Secure`).
- Mỗi request kiểm tra lại: tài khoản/doanh nghiệp bị khóa hoặc thành viên bị vô hiệu hóa, đổi vai trò → bị đăng xuất ngay.
- Chống CSRF: `AutoValidateAntiforgeryToken` cho mọi POST; AJAX gửi token qua header `RequestVerificationToken`.
- Chống XSS: Razor tự mã hóa HTML; nội dung chat/thông báo chèn bằng `textContent`.
- Chống SQL Injection: chỉ truy vấn qua EF Core/LINQ.
- Chống IDOR: mã doanh nghiệp lấy từ Claims; Global Query Filter + kiểm tra quyền trên từng đối tượng.

## Khác biệt nhỏ so với thiết kế CSDL trong báo cáo

- `NguoiDung` thêm cột `LaSuperAdmin` (đánh dấu quản trị hệ thống) và `NgayTao`.
- `DoanhNghiep` thêm cột `NgayTao` (dùng cho thống kê của Super Admin).
- `NhomLamViec` thêm cột `DaDong` để "đóng (archive)" nhóm theo R09.

Nên cập nhật mục 3.2.2 của báo cáo theo các cột này.
