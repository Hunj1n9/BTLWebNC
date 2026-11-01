using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkHub.Data;
using WorkHub.Services;

namespace WorkHub.Controllers
{
    // Thông báo của người dùng hiện tại (R11, R12)
    [Authorize]
    public class NotificationController : Controller
    {
        private readonly AppDbContext _db;
        private readonly CurrentUser _me;

        public NotificationController(AppDbContext db, CurrentUser me)
        {
            _db = db;
            _me = me;
        }

        // Trang xem tất cả thông báo
        public async Task<IActionResult> Index()
        {
            var list = await _db.ThongBaos
                .Where(x => x.MaNguoiNhan == _me.UserId)
                .OrderByDescending(x => x.ThoiGian)
                .Take(100)
                .ToListAsync();
            return View(list);
        }

        // AJAX: số thông báo chưa đọc + 10 thông báo mới nhất (cho quả chuông)
        [HttpGet]
        public async Task<IActionResult> GetUnread()
        {
            int userId = _me.UserId;
            int soChuaDoc = await _db.ThongBaos.CountAsync(x => x.MaNguoiNhan == userId && !x.TrangThaiDaDoc);
            var items = await _db.ThongBaos
                .Where(x => x.MaNguoiNhan == userId)
                .OrderBy(x => x.TrangThaiDaDoc).ThenByDescending(x => x.ThoiGian)
                .Take(10)
                .Select(x => new
                {
                    maThongBao = x.MaThongBao,
                    noiDung = x.NoiDung,
                    lienKet = x.LienKet,
                    daDoc = x.TrangThaiDaDoc,
                    thoiGian = x.ThoiGian.ToString("dd/MM HH:mm")
                })
                .ToListAsync();
            return Json(new { soChuaDoc, items });
        }

        // AJAX: đánh dấu 1 thông báo đã đọc
        [HttpPost]
        public async Task<IActionResult> MarkAsRead(int maThongBao)
        {
            // Chỉ cho đánh dấu thông báo của chính mình
            var tb = await _db.ThongBaos.FirstOrDefaultAsync(x => x.MaThongBao == maThongBao && x.MaNguoiNhan == _me.UserId);
            if (tb == null) return Json(new { success = false });

            tb.TrangThaiDaDoc = true;
            await _db.SaveChangesAsync();
            return Json(new { success = true });
        }

        // AJAX: đánh dấu tất cả đã đọc
        [HttpPost]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var list = await _db.ThongBaos.Where(x => x.MaNguoiNhan == _me.UserId && !x.TrangThaiDaDoc).ToListAsync();
            list.ForEach(x => x.TrangThaiDaDoc = true);
            await _db.SaveChangesAsync();
            return Json(new { success = true });
        }
    }
}
