using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkHub.Data;
using WorkHub.Models;
using WorkHub.Models.ViewModels;
using WorkHub.Services;

namespace WorkHub.Controllers
{
    // Quản lý thành viên doanh nghiệp - chỉ TenantAdmin
    [Authorize(Roles = VaiTros.TenantAdmin)]
    public class MemberController : Controller
    {
        private readonly AppDbContext _db;
        private readonly CurrentUser _me;
        private readonly NotificationService _notify;

        public MemberController(AppDbContext db, CurrentUser me, NotificationService notify)
        {
            _db = db;
            _me = me;
            _notify = notify;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.Invite = new InviteViewModel();
            return View(await TimKiem(null));
        }

        // AJAX: tìm kiếm thành viên theo tên / email (R14)
        [HttpGet]
        public async Task<IActionResult> Search(string q)
        {
            return PartialView("_MemberList", await TimKiem(q));
        }

        private Task<List<ThanhVienDoanhNghiep>> TimKiem(string q)
        {
            var query = _db.ThanhVienDoanhNghieps
                .Include(x => x.NguoiDung)
                .Include(x => x.ThanhVienNhoms).ThenInclude(x => x.NhomLamViec)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
                query = query.Where(x => x.NguoiDung.HoTen.Contains(q) || x.NguoiDung.Email.Contains(q));

            return query.OrderBy(x => x.NguoiDung.HoTen).ToListAsync();
        }

        // ===== Mời / thêm thành viên (BM04) =====
        [HttpPost]
        public async Task<IActionResult> Invite(InviteViewModel model)
        {
            if (model.VaiTro != VaiTros.TenantAdmin && model.VaiTro != VaiTros.Member)
                ModelState.AddModelError(nameof(model.VaiTro), "Vai trò không hợp lệ.");

            string email = model.Email?.Trim().ToLower();
            var user = await _db.NguoiDungs.FirstOrDefaultAsync(x => x.Email == email);

            // Email chưa có tài khoản thì cần họ tên và mật khẩu tạm để tạo tài khoản mới
            if (user == null)
            {
                if (string.IsNullOrWhiteSpace(model.HoTen))
                    ModelState.AddModelError(nameof(model.HoTen), "Email chưa có tài khoản, vui lòng nhập họ tên.");
                if (string.IsNullOrWhiteSpace(model.MatKhauTam))
                    ModelState.AddModelError(nameof(model.MatKhauTam), "Email chưa có tài khoản, vui lòng nhập mật khẩu tạm.");
            }
            else if (user.LaSuperAdmin)
            {
                ModelState.AddModelError(nameof(model.Email), "Không thể thêm tài khoản quản trị hệ thống.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Invite = model;
                return View("Index", await TimKiem(null));
            }

            if (user == null)
            {
                user = new NguoiDung { HoTen = model.HoTen.Trim(), Email = email, MatKhauHash = AuthService.HashPassword(model.MatKhauTam) };
                _db.NguoiDungs.Add(user);
                await _db.SaveChangesAsync();
            }

            var member = await _db.ThanhVienDoanhNghieps.FirstOrDefaultAsync(x => x.MaNguoiDung == user.MaNguoiDung);
            if (member != null && member.TrangThai)
            {
                TempData["Error"] = "Người dùng này đã là thành viên của doanh nghiệp.";
                return RedirectToAction(nameof(Index));
            }

            if (member == null)
            {
                member = new ThanhVienDoanhNghiep { MaNguoiDung = user.MaNguoiDung, MaDoanhNghiep = _me.TenantId.Value };
                _db.ThanhVienDoanhNghieps.Add(member);
            }
            member.TrangThai = true;
            member.VaiTro = model.VaiTro;

            string tenDN = User.FindFirst(AuthService.TenantNameClaim)?.Value;
            _notify.Gui(user.MaNguoiDung, "Bạn đã được thêm vào doanh nghiệp " + tenDN, "/Tenant/Switch");
            await _db.SaveChangesAsync();

            TempData["Message"] = "Đã thêm " + user.HoTen + " vào doanh nghiệp.";
            return RedirectToAction(nameof(Index));
        }

        // ===== Đổi vai trò =====
        [HttpPost]
        public async Task<IActionResult> ChangeRole(int maThanhVien, string vaiTro)
        {
            var member = await _db.ThanhVienDoanhNghieps.FindAsync(maThanhVien);
            if (member == null) return NotFound();

            if (member.MaThanhVien == _me.MemberId)
                TempData["Error"] = "Bạn không thể tự đổi vai trò của mình.";
            else if (vaiTro != VaiTros.TenantAdmin && vaiTro != VaiTros.Member)
                TempData["Error"] = "Vai trò không hợp lệ.";
            else
            {
                member.VaiTro = vaiTro;
                await _db.SaveChangesAsync();
                TempData["Message"] = "Đã đổi vai trò.";
            }
            return RedirectToAction(nameof(Index));
        }

        // ===== Vô hiệu hóa / kích hoạt lại thành viên =====
        [HttpPost]
        public async Task<IActionResult> Deactivate(int maThanhVien)
        {
            var member = await _db.ThanhVienDoanhNghieps.FindAsync(maThanhVien);
            if (member == null) return NotFound();

            if (member.MaThanhVien == _me.MemberId)
            {
                TempData["Error"] = "Bạn không thể tự vô hiệu hóa chính mình.";
            }
            else
            {
                member.TrangThai = !member.TrangThai;
                await _db.SaveChangesAsync();
                TempData["Message"] = member.TrangThai ? "Đã kích hoạt lại thành viên." : "Đã vô hiệu hóa thành viên.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
