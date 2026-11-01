using System.Security.Claims;
using WorkHub.Models;

namespace WorkHub.Services
{
    // Đọc thông tin người dùng hiện tại từ Cookie đăng nhập (Claims)
    public class CurrentUser
    {
        private readonly IHttpContextAccessor _accessor;

        public CurrentUser(IHttpContextAccessor accessor)
        {
            _accessor = accessor;
        }

        private ClaimsPrincipal User => _accessor.HttpContext?.User;

        public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;

        public int UserId => GetInt(ClaimTypes.NameIdentifier) ?? 0;
        public int? TenantId => GetInt(AuthService.TenantIdClaim);
        public int? MemberId => GetInt(AuthService.MemberIdClaim);
        public string HoTen => User?.Identity?.Name;
        public string Role => User?.FindFirstValue(ClaimTypes.Role);

        public bool IsSuperAdmin => Role == VaiTros.SuperAdmin;
        public bool IsTenantAdmin => Role == VaiTros.TenantAdmin;

        private int? GetInt(string type)
        {
            var value = User?.FindFirstValue(type);
            return int.TryParse(value, out var n) ? n : null;
        }
    }
}
