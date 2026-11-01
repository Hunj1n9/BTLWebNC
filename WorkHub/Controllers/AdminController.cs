using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkHub.Data;
using WorkHub.Models;
using WorkHub.Models.ViewModels;
using WorkHub.Services;

namespace WorkHub.Controllers
{
    // Khu vực quản trị nền tảng - chỉ Super Admin (R17, R18)
    // Cần xem dữ liệu của mọi doanh nghiệp nên dùng IgnoreQueryFilters()
    [Authorize(Roles = VaiTros.SuperAdmin)]
    public class AdminController : Controller
    {
        private readonly AppDbContext _db;
        private readonly CurrentUser _me;

        public AdminController(AppDbContext db, CurrentUser me)
        {
            _db = db;
            _me = me;
        }

        public async Task<IActionResult> Dashboard()
        {
            var model = new AdminDashboardViewModel
            {
                SoNguoiDung = await _db.NguoiDungs.CountAsync(),
                SoDoanhNghiep = await _db.DoanhNghieps.CountAsync(),
                SoDoanhNghiepBiKhoa = await _db.DoanhNghieps.CountAsync(x => !x.TrangThai),
                SoNhom = await _db.NhomLamViecs.IgnoreQueryFilters().CountAsync(),
                SoCongViec = await _db.CongViecs.IgnoreQueryFilters().CountAsync(),
                DoanhNghiepMoi = await _db.DoanhNghieps.OrderByDescending(x => x.NgayTao).Take(5).ToListAsync()
            };
            return View(model);
        }

        // Danh sách doanh nghiệp kèm số thành viên, số nhóm
        public async Task<IActionResult> Tenants(string q)
        {
            var query = _db.DoanhNghieps.AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
                query = query.Where(x => x.TenDoanhNghiep.Contains(q) || x.Email.Contains(q));

            var list = await query
                .OrderByDescending(x => x.NgayTao)
                .Select(x => new TenantRow
                {
                    DoanhNghiep = x,
                    SoThanhVien = _db.ThanhVienDoanhNghieps.IgnoreQueryFilters().Count(t => t.MaDoanhNghiep == x.MaDoanhNghiep),
                    SoNhom = _db.NhomLamViecs.IgnoreQueryFilters().Count(n => n.MaDoanhNghiep == x.MaDoanhNghiep)
                })
                .ToListAsync();
            ViewBag.Q = q;
            return View(list);
        }

        [HttpPost]
        public async Task<IActionResult> SetTenantStatus(int id)
        {
            var dn = await _db.DoanhNghieps.FindAsync(id);
            if (dn == null) return NotFound();

            dn.TrangThai = !dn.TrangThai;
            await _db.SaveChangesAsync();
            TempData["Message"] = (dn.TrangThai ? "Đã mở khóa " : "Đã khóa ") + dn.TenDoanhNghiep;
            return RedirectToAction(nameof(Tenants));
        }

        public async Task<IActionResult> Users(string q)
        {
            var query = _db.NguoiDungs.AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
                query = query.Where(x => x.HoTen.Contains(q) || x.Email.Contains(q));

            ViewBag.Q = q;
            return View(await query.OrderBy(x => x.HoTen).ToListAsync());
        }

        [HttpPost]
        public async Task<IActionResult> SetUserStatus(int id)
        {
            var user = await _db.NguoiDungs.FindAsync(id);
            if (user == null) return NotFound();

            if (user.MaNguoiDung == _me.UserId)
            {
                TempData["Error"] = "Không thể tự khóa tài khoản của mình.";
            }
            else
            {
                user.TrangThai = !user.TrangThai;
                await _db.SaveChangesAsync();
                TempData["Message"] = (user.TrangThai ? "Đã mở khóa " : "Đã khóa ") + user.Email;
            }
            return RedirectToAction(nameof(Users));
        }
    }

    public class TenantRow
    {
        public DoanhNghiep DoanhNghiep { get; set; }
        public int SoThanhVien { get; set; }
        public int SoNhom { get; set; }
    }
}
