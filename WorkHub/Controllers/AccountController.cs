using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using WorkHub.Data;
using WorkHub.Models;
using WorkHub.Models.ViewModels;
using WorkHub.Services;

namespace WorkHub.Controllers
{
    public class AccountController : Controller
    {
        private const int SoLanSaiToiDa = 5;

        private readonly AppDbContext _db;
        private readonly CurrentUser _me;
        private readonly IMemoryCache _cache;

        public AccountController(AppDbContext db, CurrentUser me, IMemoryCache cache)
        {
            _db = db;
            _me = me;
            _cache = cache;
        }

        // ===== Đăng nhập (BM02) =====
        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            if (_me.IsAuthenticated) return RedirectToAction("Dashboard", "Home");
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            string email = model.Email.Trim().ToLower();
            string cacheKey = "login_fail_" + email;
            int soLanSai = _cache.GetOrCreate(cacheKey, e => 0);

            // Chống dò mật khẩu: sai quá 5 lần thì tạm khóa 5 phút
            if (soLanSai >= SoLanSaiToiDa)
            {
                ModelState.AddModelError("", "Bạn đã nhập sai quá nhiều lần. Vui lòng thử lại sau 5 phút.");
                return View(model);
            }

            var user = await _db.NguoiDungs.FirstOrDefaultAsync(x => x.Email == email);
            if (user == null || !AuthService.VerifyPassword(user.MatKhauHash, model.MatKhau))
            {
                _cache.Set(cacheKey, soLanSai + 1, TimeSpan.FromMinutes(5));
                // Thông báo chung, không tiết lộ email có tồn tại hay không
                ModelState.AddModelError("", "Email hoặc mật khẩu không đúng.");
                return View(model);
            }

            if (!user.TrangThai)
            {
                ModelState.AddModelError("", "Tài khoản của bạn đã bị khóa.");
                return View(model);
            }

            _cache.Remove(cacheKey);

            var member = user.LaSuperAdmin ? null : await AuthService.FindDefaultMembershipAsync(_db, user.MaNguoiDung);
            await AuthService.SignInAsync(HttpContext, user, member);

            if (user.LaSuperAdmin) return RedirectToAction("Dashboard", "Admin");
            if (member == null)
            {
                TempData["Message"] = "Bạn chưa thuộc doanh nghiệp nào đang hoạt động. Hãy tạo doanh nghiệp mới hoặc nhờ quản trị doanh nghiệp mời bạn.";
                return RedirectToAction("Create", "Tenant");
            }
            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                return Redirect(model.ReturnUrl);
            return RedirectToAction("Dashboard", "Home");
        }

        // ===== Đăng ký (BM01) =====
        [HttpGet]
        public IActionResult Register()
        {
            if (_me.IsAuthenticated) return RedirectToAction("Dashboard", "Home");
            return View(new RegisterViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            string email = model.Email?.Trim().ToLower();
            if (ModelState.IsValid && await _db.NguoiDungs.AnyAsync(x => x.Email == email))
                ModelState.AddModelError(nameof(model.Email), "Email này đã được sử dụng.");

            if (!ModelState.IsValid) return View(model);

            var user = new NguoiDung
            {
                HoTen = model.HoTen.Trim(),
                Email = email,
                MatKhauHash = AuthService.HashPassword(model.MatKhau)
            };
            _db.NguoiDungs.Add(user);

            ThanhVienDoanhNghiep member = null;
            if (!string.IsNullOrWhiteSpace(model.TenDoanhNghiep))
            {
                // Tạo doanh nghiệp mới, người đăng ký là quản trị doanh nghiệp
                var dn = new DoanhNghiep { TenDoanhNghiep = model.TenDoanhNghiep.Trim(), Email = email };
                member = new ThanhVienDoanhNghiep { NguoiDung = user, DoanhNghiep = dn, VaiTro = VaiTros.TenantAdmin };
                _db.ThanhVienDoanhNghieps.Add(member);
            }

            await _db.SaveChangesAsync();
            await AuthService.SignInAsync(HttpContext, user, member);

            if (member == null) return RedirectToAction("Create", "Tenant");
            TempData["Message"] = "Đăng ký thành công!";
            return RedirectToAction("Dashboard", "Home");
        }

        // ===== Đăng xuất =====
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        // ===== Hồ sơ cá nhân (BM09) =====
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var user = await _db.NguoiDungs.FindAsync(_me.UserId);
            ViewBag.ChangePassword = new ChangePasswordViewModel();
            return View(new ProfileViewModel { HoTen = user.HoTen, Email = user.Email });
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            var user = await _db.NguoiDungs.FindAsync(_me.UserId);
            model.Email = user.Email; // email chỉ xem, không cho sửa

            if (!ModelState.IsValid)
            {
                ViewBag.ChangePassword = new ChangePasswordViewModel();
                return View(model);
            }

            user.HoTen = model.HoTen.Trim();
            await _db.SaveChangesAsync();
            await AuthService.RefreshSignInAsync(HttpContext, _db, user.MaNguoiDung, _me.MemberId);

            TempData["Message"] = "Đã cập nhật hồ sơ.";
            return RedirectToAction(nameof(Profile));
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            var user = await _db.NguoiDungs.FindAsync(_me.UserId);

            if (ModelState.IsValid && !AuthService.VerifyPassword(user.MatKhauHash, model.MatKhauCu))
                ModelState.AddModelError(nameof(model.MatKhauCu), "Mật khẩu hiện tại không đúng.");

            if (!ModelState.IsValid)
            {
                ViewBag.ChangePassword = model;
                return View("Profile", new ProfileViewModel { HoTen = user.HoTen, Email = user.Email });
            }

            user.MatKhauHash = AuthService.HashPassword(model.MatKhauMoi);
            await _db.SaveChangesAsync();

            TempData["Message"] = "Đổi mật khẩu thành công.";
            return RedirectToAction(nameof(Profile));
        }

        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
