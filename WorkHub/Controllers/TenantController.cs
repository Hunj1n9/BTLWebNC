using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkHub.Data;
using WorkHub.Models;
using WorkHub.Models.ViewModels;
using WorkHub.Services;

namespace WorkHub.Controllers
{
    [Authorize]
    public class TenantController : Controller
    {
        private const long LogoMaxBytes = 2 * 1024 * 1024; // 2MB
        private static readonly string[] LogoExtensions = { ".jpg", ".jpeg", ".png" };

        private readonly AppDbContext _db;
        private readonly CurrentUser _me;
        private readonly IWebHostEnvironment _env;

        public TenantController(AppDbContext db, CurrentUser me, IWebHostEnvironment env)
        {
            _db = db;
            _me = me;
            _env = env;
        }

        // ===== Tạo doanh nghiệp mới (R06) =====
        [HttpGet]
        public IActionResult Create()
        {
            if (_me.IsSuperAdmin) return RedirectToAction("Dashboard", "Admin");
            return View(new TenantViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Create(TenantViewModel model)
        {
            if (_me.IsSuperAdmin) return Forbid();
            if (!ModelState.IsValid) return View(model);

            var dn = new DoanhNghiep
            {
                TenDoanhNghiep = model.TenDoanhNghiep.Trim(),
                Email = model.Email,
                SoDienThoai = model.SoDienThoai,
                DiaChi = model.DiaChi,
                MoTa = model.MoTa
            };
            var member = new ThanhVienDoanhNghiep { MaNguoiDung = _me.UserId, DoanhNghiep = dn, VaiTro = VaiTros.TenantAdmin };
            _db.ThanhVienDoanhNghieps.Add(member);
            await _db.SaveChangesAsync();

            // Chuyển sang làm việc với doanh nghiệp vừa tạo
            await AuthService.RefreshSignInAsync(HttpContext, _db, _me.UserId, member.MaThanhVien);
            TempData["Message"] = "Đã tạo doanh nghiệp " + dn.TenDoanhNghiep;
            return RedirectToAction("Dashboard", "Home");
        }

        // ===== Cập nhật thông tin & logo doanh nghiệp (R07) =====
        [HttpGet]
        [Authorize(Roles = VaiTros.TenantAdmin)]
        public async Task<IActionResult> Settings()
        {
            var dn = await _db.DoanhNghieps.FindAsync(_me.TenantId);
            return View(new TenantViewModel
            {
                MaDoanhNghiep = dn.MaDoanhNghiep,
                TenDoanhNghiep = dn.TenDoanhNghiep,
                Email = dn.Email,
                SoDienThoai = dn.SoDienThoai,
                DiaChi = dn.DiaChi,
                MoTa = dn.MoTa,
                LogoUrl = dn.LogoUrl
            });
        }

        [HttpPost]
        [Authorize(Roles = VaiTros.TenantAdmin)]
        public async Task<IActionResult> Settings(TenantViewModel model)
        {
            // Luôn lấy doanh nghiệp theo Claims, không tin mã do form gửi lên
            var dn = await _db.DoanhNghieps.FindAsync(_me.TenantId);
            model.LogoUrl = dn.LogoUrl;

            // Kiểm tra file logo: chỉ JPG/PNG, tối đa 2MB
            string ext = null;
            if (model.Logo != null)
            {
                ext = Path.GetExtension(model.Logo.FileName).ToLower();
                if (!LogoExtensions.Contains(ext))
                    ModelState.AddModelError(nameof(model.Logo), "Chỉ chấp nhận file JPG hoặc PNG.");
                else if (model.Logo.Length > LogoMaxBytes)
                    ModelState.AddModelError(nameof(model.Logo), "File logo tối đa 2MB.");
            }

            if (!ModelState.IsValid) return View(model);

            dn.TenDoanhNghiep = model.TenDoanhNghiep.Trim();
            dn.Email = model.Email;
            dn.SoDienThoai = model.SoDienThoai;
            dn.DiaChi = model.DiaChi;
            dn.MoTa = model.MoTa;

            if (model.Logo != null)
            {
                // Đặt tên file ngẫu nhiên để tránh trùng và tránh tên file độc hại
                string folder = Path.Combine(_env.WebRootPath, "uploads");
                Directory.CreateDirectory(folder);
                string fileName = Guid.NewGuid().ToString("N") + ext;
                using (var stream = new FileStream(Path.Combine(folder, fileName), FileMode.Create))
                {
                    await model.Logo.CopyToAsync(stream);
                }
                dn.LogoUrl = "/uploads/" + fileName;
            }

            await _db.SaveChangesAsync();
            await AuthService.RefreshSignInAsync(HttpContext, _db, _me.UserId, _me.MemberId);

            TempData["Message"] = "Đã lưu thông tin doanh nghiệp.";
            return RedirectToAction(nameof(Settings));
        }

        // ===== Chọn doanh nghiệp làm việc (khi người dùng thuộc nhiều doanh nghiệp) =====
        [HttpGet]
        public async Task<IActionResult> Switch()
        {
            var list = await _db.ThanhVienDoanhNghieps.IgnoreQueryFilters()
                .Include(x => x.DoanhNghiep)
                .Where(x => x.MaNguoiDung == _me.UserId)
                .ToListAsync();
            return View(list);
        }

        [HttpPost]
        public async Task<IActionResult> Switch(int maThanhVien)
        {
            var member = await _db.ThanhVienDoanhNghieps.IgnoreQueryFilters()
                .Include(x => x.DoanhNghiep)
                .FirstOrDefaultAsync(x => x.MaThanhVien == maThanhVien && x.MaNguoiDung == _me.UserId);

            if (member == null || !member.TrangThai || !member.DoanhNghiep.TrangThai)
            {
                TempData["Error"] = "Không thể chuyển sang doanh nghiệp này.";
                return RedirectToAction(nameof(Switch));
            }

            await AuthService.RefreshSignInAsync(HttpContext, _db, _me.UserId, member.MaThanhVien);
            return RedirectToAction("Dashboard", "Home");
        }
    }
}
