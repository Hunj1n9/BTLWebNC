using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkHub.Data;
using WorkHub.Models;
using WorkHub.Services;

namespace WorkHub.Controllers
{
    // Trao đổi trong nhóm và bình luận công việc - toàn bộ qua AJAX (R04, R05)
    [Authorize(Roles = VaiTros.TenantAdmin + "," + VaiTros.Member)]
    public class CommentController : Controller
    {
        private const int DoDaiToiDa = 2000;

        private readonly AppDbContext _db;
        private readonly CurrentUser _me;
        private readonly NotificationService _notify;

        public CommentController(AppDbContext db, CurrentUser me, NotificationService notify)
        {
            _db = db;
            _me = me;
            _notify = notify;
        }

        // GET /Comment/List?maCongViec=1  hoặc  /Comment/List?maNhom=1
        [HttpGet]
        public async Task<IActionResult> List(int? maCongViec, int? maNhom)
        {
            if (!await CoQuyen(maCongViec, maNhom)) return Forbid();

            var query = _db.TraoDoiBinhLuans.AsQueryable();
            query = maCongViec != null
                ? query.Where(x => x.MaCongViec == maCongViec)
                : query.Where(x => x.MaNhom == maNhom && x.MaCongViec == null);

            var list = await query
                .OrderBy(x => x.ThoiGian)
                .Select(x => new
                {
                    maBinhLuan = x.MaBinhLuan,
                    nguoiGui = x.NguoiGui.NguoiDung.HoTen,
                    laCuaToi = x.MaNguoiGui == _me.MemberId,
                    noiDung = x.NoiDung,
                    thoiGian = x.ThoiGian.ToString("dd/MM/yyyy HH:mm")
                })
                .ToListAsync();
            return Json(list);
        }

        // POST /Comment/Post
        [HttpPost]
        public async Task<IActionResult> Post(string noiDung, int? maCongViec, int? maNhom)
        {
            noiDung = noiDung?.Trim();
            if (string.IsNullOrEmpty(noiDung))
                return Json(new { success = false, message = "Nội dung không được để trống." });
            if (noiDung.Length > DoDaiToiDa)
                return Json(new { success = false, message = "Nội dung tối đa " + DoDaiToiDa + " ký tự." });
            if (!await CoQuyen(maCongViec, maNhom))
                return Json(new { success = false, message = "Bạn không có quyền gửi tin ở đây." });

            // Bình luận công việc thì không gắn MaNhom, tin nhắn nhóm thì không gắn MaCongViec
            var bl = new TraoDoiBinhLuan
            {
                MaNguoiGui = _me.MemberId.Value,
                NoiDung = noiDung,
                ThoiGian = DateTime.Now,
                MaCongViec = maCongViec,
                MaNhom = maCongViec == null ? maNhom : null
            };
            _db.TraoDoiBinhLuans.Add(bl);

            await TaoThongBao(bl);
            await _db.SaveChangesAsync();

            return Json(new
            {
                success = true,
                maBinhLuan = bl.MaBinhLuan,
                nguoiGui = _me.HoTen,
                laCuaToi = true,
                noiDung = bl.NoiDung,
                thoiGian = bl.ThoiGian.ToString("dd/MM/yyyy HH:mm")
            });
        }

        // Kiểm tra người dùng có quyền xem/gửi ở công việc hoặc nhóm này không
        private async Task<bool> CoQuyen(int? maCongViec, int? maNhom)
        {
            if (maCongViec != null)
                return await QuyenHelper.CongViecDuocXem(_db, _me).AnyAsync(x => x.MaCongViec == maCongViec);
            if (maNhom != null)
                return await QuyenHelper.NhomDuocXem(_db, _me).AnyAsync(x => x.MaNhom == maNhom);
            return false;
        }

        // Thông báo bình luận / tin nhắn mới và nhắc tên @Họ Tên (R12)
        private async Task TaoThongBao(TraoDoiBinhLuan bl)
        {
            var nguoiNhan = new Dictionary<int, string>(); // MaNguoiDung -> nội dung
            string link;

            if (bl.MaCongViec != null)
            {
                var cv = await _db.CongViecs
                    .Include(x => x.NguoiGiao).Include(x => x.NguoiThucHien)
                    .FirstAsync(x => x.MaCongViec == bl.MaCongViec);
                link = "/Task/Details/" + cv.MaCongViec;
                string nd = _me.HoTen + " đã bình luận trong công việc: " + cv.TenCongViec;
                nguoiNhan[cv.NguoiGiao.MaNguoiDung] = nd;
                nguoiNhan[cv.NguoiThucHien.MaNguoiDung] = nd;
            }
            else
            {
                var nhom = await _db.NhomLamViecs.FindAsync(bl.MaNhom);
                link = "/Group/Details/" + nhom.MaNhom;
                var thanhViens = await _db.ThanhVienNhoms.Where(x => x.MaNhom == nhom.MaNhom)
                    .Select(x => x.ThanhVien.MaNguoiDung).ToListAsync();
                foreach (var id in thanhViens)
                    nguoiNhan[id] = _me.HoTen + " đã gửi tin nhắn trong nhóm " + nhom.TenNhom;
            }

            // Nhắc tên: nội dung có "@Họ Tên" của thành viên trong doanh nghiệp
            if (bl.NoiDung.Contains('@'))
            {
                var members = await _db.ThanhVienDoanhNghieps.Where(x => x.TrangThai)
                    .Select(x => new { x.MaNguoiDung, x.NguoiDung.HoTen }).ToListAsync();
                foreach (var m in members.Where(m => bl.NoiDung.Contains("@" + m.HoTen)))
                    nguoiNhan[m.MaNguoiDung] = _me.HoTen + " đã nhắc đến bạn";
            }

            foreach (var item in nguoiNhan)
                _notify.Gui(item.Key, item.Value, link);
        }
    }
}
