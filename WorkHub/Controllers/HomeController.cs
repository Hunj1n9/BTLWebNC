using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkHub.Data;
using WorkHub.Models;
using WorkHub.Models.ViewModels;
using WorkHub.Services;

namespace WorkHub.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _db;
        private readonly CurrentUser _me;

        public HomeController(AppDbContext db, CurrentUser me)
        {
            _db = db;
            _me = me;
        }

        // Trang chủ công khai (SEO)
        public IActionResult Index()
        {
            return View();
        }

        // Trang giới thiệu công khai (SEO)
        public IActionResult About()
        {
            return View();
        }

        // Bảng điều khiển sau khi đăng nhập
        [Authorize]
        public async Task<IActionResult> Dashboard()
        {
            if (_me.IsSuperAdmin) return RedirectToAction("Dashboard", "Admin");
            if (_me.TenantId == null) return RedirectToAction("Create", "Tenant");

            var congViecs = QuyenHelper.CongViecDuocXem(_db, _me);
            int? memberId = _me.MemberId;

            var model = new DashboardViewModel
            {
                TenDoanhNghiep = User.FindFirst(AuthService.TenantNameClaim)?.Value,
                SoThanhVien = await _db.ThanhVienDoanhNghieps.CountAsync(x => x.TrangThai),
                SoNhom = await QuyenHelper.NhomDuocXem(_db, _me).CountAsync(x => !x.DaDong),
                SoChuaLam = await congViecs.CountAsync(x => x.TrangThai == TrangThaiCV.ChuaLam),
                SoDangLam = await congViecs.CountAsync(x => x.TrangThai == TrangThaiCV.DangLam),
                SoHoanThanh = await congViecs.CountAsync(x => x.TrangThai == TrangThaiCV.HoanThanh),
                SoQuaHan = await congViecs.CountAsync(x => x.TrangThai != TrangThaiCV.HoanThanh && x.HanHoanThanh < DateTime.Today),
                CongViecCuaToi = await _db.CongViecs
                    .Include(x => x.NhomLamViec)
                    .Where(x => x.MaNguoiThucHien == memberId && x.TrangThai != TrangThaiCV.HoanThanh)
                    .OrderBy(x => x.HanHoanThanh)
                    .Take(10)
                    .ToListAsync(),
                TienDoNhoms = await QuyenHelper.NhomDuocXem(_db, _me)
                    .Where(x => !x.DaDong)
                    .Select(x => new TienDoNhom
                    {
                        MaNhom = x.MaNhom,
                        TenNhom = x.TenNhom,
                        Tong = x.CongViecs.Count(),
                        HoanThanh = x.CongViecs.Count(c => c.TrangThai == TrangThaiCV.HoanThanh)
                    })
                    .OrderBy(x => x.TenNhom)
                    .ToListAsync()
            };
            return View(model);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
