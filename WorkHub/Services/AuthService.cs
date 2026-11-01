using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WorkHub.Data;
using WorkHub.Models;

namespace WorkHub.Services
{
    // Các hàm liên quan tới đăng nhập: băm mật khẩu, tạo Claims, chọn doanh nghiệp
    public class AuthService
    {
        public const string TenantIdClaim = "TenantId";
        public const string MemberIdClaim = "MemberId";
        public const string TenantNameClaim = "TenantName";
        public const string TenantLogoClaim = "TenantLogo";

        private static readonly PasswordHasher<NguoiDung> _hasher = new();

        // Băm mật khẩu (PBKDF2 có salt - có sẵn trong ASP.NET Core)
        public static string HashPassword(string password) => _hasher.HashPassword(null, password);

        public static bool VerifyPassword(string hash, string password) =>
            _hasher.VerifyHashedPassword(null, hash, password) != PasswordVerificationResult.Failed;

        // Lấy tư cách thành viên đầu tiên còn hoạt động của người dùng (bỏ qua bộ lọc doanh nghiệp)
        public static Task<ThanhVienDoanhNghiep> FindDefaultMembershipAsync(AppDbContext db, int userId)
        {
            return db.ThanhVienDoanhNghieps.IgnoreQueryFilters()
                .Include(x => x.DoanhNghiep)
                .Where(x => x.MaNguoiDung == userId && x.TrangThai && x.DoanhNghiep.TrangThai)
                .OrderBy(x => x.MaThanhVien)
                .FirstOrDefaultAsync();
        }

        // Đăng nhập: ghi Cookie chứa mã người dùng, vai trò và doanh nghiệp đang làm việc
        public static async Task SignInAsync(HttpContext http, NguoiDung user, ThanhVienDoanhNghiep member)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.MaNguoiDung.ToString()),
                new Claim(ClaimTypes.Name, user.HoTen),
                new Claim(ClaimTypes.Email, user.Email),
            };

            if (user.LaSuperAdmin)
            {
                claims.Add(new Claim(ClaimTypes.Role, VaiTros.SuperAdmin));
            }
            else if (member != null)
            {
                claims.Add(new Claim(ClaimTypes.Role, member.VaiTro));
                claims.Add(new Claim(TenantIdClaim, member.MaDoanhNghiep.ToString()));
                claims.Add(new Claim(MemberIdClaim, member.MaThanhVien.ToString()));
                claims.Add(new Claim(TenantNameClaim, member.DoanhNghiep.TenDoanhNghiep));
                if (!string.IsNullOrEmpty(member.DoanhNghiep.LogoUrl))
                    claims.Add(new Claim(TenantLogoClaim, member.DoanhNghiep.LogoUrl));
            }

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
        }

        // Đăng nhập lại để cập nhật Claims (sau khi đổi họ tên, tên doanh nghiệp...)
        public static async Task RefreshSignInAsync(HttpContext http, AppDbContext db, int userId, int? memberId)
        {
            var user = await db.NguoiDungs.FindAsync(userId);
            var member = memberId == null ? null : await db.ThanhVienDoanhNghieps.IgnoreQueryFilters()
                .Include(x => x.DoanhNghiep)
                .FirstOrDefaultAsync(x => x.MaThanhVien == memberId);
            await SignInAsync(http, user, member);
        }

        // Chạy mỗi request: nếu tài khoản / thành viên / doanh nghiệp bị khóa hoặc đổi vai trò thì buộc đăng nhập lại
        public static async Task ValidatePrincipalAsync(CookieValidatePrincipalContext context)
        {
            var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
            var principal = context.Principal;

            int.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId);
            var user = await db.NguoiDungs.FindAsync(userId);
            bool valid = user != null && user.TrangThai;

            var memberClaim = principal.FindFirstValue(MemberIdClaim);
            if (valid && memberClaim != null)
            {
                int memberId = int.Parse(memberClaim);
                var member = await db.ThanhVienDoanhNghieps.IgnoreQueryFilters()
                    .Include(x => x.DoanhNghiep)
                    .FirstOrDefaultAsync(x => x.MaThanhVien == memberId);
                valid = member != null && member.TrangThai && member.DoanhNghiep.TrangThai
                        && member.VaiTro == principal.FindFirstValue(ClaimTypes.Role);
            }

            if (!valid)
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
        }
    }
}
