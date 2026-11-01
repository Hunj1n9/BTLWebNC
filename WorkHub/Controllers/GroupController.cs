using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WorkHub.Data;
using WorkHub.Models;
using WorkHub.Models.ViewModels;
using WorkHub.Services;

namespace WorkHub.Controllers
{
    [Authorize(Roles = VaiTros.TenantAdmin + "," + VaiTros.Member)]
    public class GroupController : Controller
    {
        private readonly AppDbContext _db;
        private readonly CurrentUser _me;
        private readonly NotificationService _notify;

        public GroupController(AppDbContext db, CurrentUser me, NotificationService notify)
        {
            _db = db;
            _me = me;
            _notify = notify;
        }

        // ===== Danh sách + lọc nhóm (R14). Gọi bằng AJAX thì chỉ trả về Partial View =====
        public async Task<IActionResult> Index(string q, string trangThai = "mo")
        {
            var query = QuyenHelper.NhomDuocXem(_db, _me)
                .Include(x => x.TruongNhom).ThenInclude(x => x.NguoiDung)
                .Include(x => x.ThanhViens)
                .Include(x => x.CongViecs)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
                query = query.Where(x => x.TenNhom.Contains(q) || x.MoTa.Contains(q));
            if (trangThai == "mo") query = query.Where(x => !x.DaDong);
            if (trangThai == "dong") query = query.Where(x => x.DaDong);

            var list = await query.OrderBy(x => x.TenNhom).ToListAsync();

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return PartialView("_GroupList", list);
            return View(list);
        }

        // ===== Chi tiết nhóm: thành viên, công việc, khung trao đổi =====
        public async Task<IActionResult> Details(int id)
        {
            var nhom = await QuyenHelper.NhomDuocXem(_db, _me)
                .Include(x => x.TruongNhom).ThenInclude(x => x.NguoiDung)
                .Include(x => x.ThanhViens).ThenInclude(x => x.ThanhVien).ThenInclude(x => x.NguoiDung)
                .Include(x => x.CongViecs).ThenInclude(x => x.NguoiThucHien).ThenInclude(x => x.NguoiDung)
                .FirstOrDefaultAsync(x => x.MaNhom == id);
            if (nhom == null) return NotFound();

            // Danh sách thành viên doanh nghiệp chưa ở trong nhóm (để Admin thêm vào)
            var daCo = nhom.ThanhViens.Select(x => x.MaThanhVien).ToList();
            ViewBag.ChuaVaoNhom = await _db.ThanhVienDoanhNghieps.Include(x => x.NguoiDung)
                .Where(x => x.TrangThai && !daCo.Contains(x.MaThanhVien))
                .OrderBy(x => x.NguoiDung.HoTen)
                .ToListAsync();
            ViewBag.CoTheGiaoViec = QuyenHelper.CoTheGiaoViec(_me, nhom);
            return View(nhom);
        }

        // ===== Tạo nhóm (BM05) =====
        [HttpGet]
        [Authorize(Roles = VaiTros.TenantAdmin)]
        public async Task<IActionResult> Create()
        {
            await LoadMembers();
            return View("Form", new GroupFormViewModel());
        }

        [HttpPost]
        [Authorize(Roles = VaiTros.TenantAdmin)]
        public async Task<IActionResult> Create(GroupFormViewModel model)
        {
            await KiemTraThanhVien(model);
            if (!ModelState.IsValid)
            {
                await LoadMembers();
                return View("Form", model);
            }

            var nhom = new NhomLamViec
            {
                TenNhom = model.TenNhom.Trim(),
                MoTa = model.MoTa,
                MaTruongNhom = model.MaTruongNhom,
                MaDoanhNghiep = _me.TenantId.Value
            };
            _db.NhomLamViecs.Add(nhom);
            await _db.SaveChangesAsync();

            await CapNhatThanhVien(nhom, model.MaThanhViens);
            await _db.SaveChangesAsync();

            TempData["Message"] = "Đã tạo nhóm " + nhom.TenNhom;
            return RedirectToAction(nameof(Details), new { id = nhom.MaNhom });
        }

        // ===== Sửa nhóm / đóng nhóm (R09) =====
        [HttpGet]
        [Authorize(Roles = VaiTros.TenantAdmin)]
        public async Task<IActionResult> Edit(int id)
        {
            var nhom = await _db.NhomLamViecs.Include(x => x.ThanhViens).FirstOrDefaultAsync(x => x.MaNhom == id);
            if (nhom == null) return NotFound();

            await LoadMembers();
            return View("Form", new GroupFormViewModel
            {
                MaNhom = nhom.MaNhom,
                TenNhom = nhom.TenNhom,
                MoTa = nhom.MoTa,
                MaTruongNhom = nhom.MaTruongNhom,
                DaDong = nhom.DaDong,
                MaThanhViens = nhom.ThanhViens.Select(x => x.MaThanhVien).ToList()
            });
        }

        [HttpPost]
        [Authorize(Roles = VaiTros.TenantAdmin)]
        public async Task<IActionResult> Edit(int id, GroupFormViewModel model)
        {
            var nhom = await _db.NhomLamViecs.Include(x => x.ThanhViens).FirstOrDefaultAsync(x => x.MaNhom == id);
            if (nhom == null) return NotFound();

            await KiemTraThanhVien(model);
            if (!ModelState.IsValid)
            {
                await LoadMembers();
                return View("Form", model);
            }

            nhom.TenNhom = model.TenNhom.Trim();
            nhom.MoTa = model.MoTa;
            nhom.MaTruongNhom = model.MaTruongNhom;
            nhom.DaDong = model.DaDong;
            await CapNhatThanhVien(nhom, model.MaThanhViens);
            await _db.SaveChangesAsync();

            TempData["Message"] = "Đã cập nhật nhóm.";
            return RedirectToAction(nameof(Details), new { id = nhom.MaNhom });
        }

        // ===== AJAX: thêm thành viên vào nhóm (R10) =====
        [HttpPost]
        [Authorize(Roles = VaiTros.TenantAdmin)]
        public async Task<IActionResult> AddMember(int maNhom, int maThanhVien)
        {
            var nhom = await _db.NhomLamViecs.FindAsync(maNhom);
            var member = await _db.ThanhVienDoanhNghieps.Include(x => x.NguoiDung)
                .FirstOrDefaultAsync(x => x.MaThanhVien == maThanhVien && x.TrangThai);
            if (nhom == null || member == null)
                return Json(new { success = false, message = "Không tìm thấy nhóm hoặc thành viên." });

            if (await _db.ThanhVienNhoms.AnyAsync(x => x.MaNhom == maNhom && x.MaThanhVien == maThanhVien))
                return Json(new { success = false, message = "Thành viên đã ở trong nhóm." });

            _db.ThanhVienNhoms.Add(new ThanhVienNhom { MaNhom = maNhom, MaThanhVien = maThanhVien });
            _notify.Gui(member.MaNguoiDung, "Bạn được thêm vào nhóm " + nhom.TenNhom, "/Group/Details/" + maNhom);
            await _db.SaveChangesAsync();

            return Json(new { success = true, maThanhVien, hoTen = member.NguoiDung.HoTen, email = member.NguoiDung.Email });
        }

        // ===== AJAX: loại thành viên khỏi nhóm (R10) =====
        [HttpPost]
        [Authorize(Roles = VaiTros.TenantAdmin)]
        public async Task<IActionResult> RemoveMember(int maNhom, int maThanhVien)
        {
            var tv = await _db.ThanhVienNhoms.FirstOrDefaultAsync(x => x.MaNhom == maNhom && x.MaThanhVien == maThanhVien);
            if (tv == null) return Json(new { success = false, message = "Thành viên không ở trong nhóm." });

            var nhom = await _db.NhomLamViecs.FindAsync(maNhom);
            if (nhom.MaTruongNhom == maThanhVien)
                return Json(new { success = false, message = "Không thể loại trưởng nhóm. Hãy đổi trưởng nhóm trước." });

            _db.ThanhVienNhoms.Remove(tv);
            await _db.SaveChangesAsync();
            return Json(new { success = true });
        }

        // ===== AJAX: danh sách thành viên của nhóm (dùng cho form giao việc) =====
        [HttpGet]
        public async Task<IActionResult> Members(int id)
        {
            if (!await QuyenHelper.NhomDuocXem(_db, _me).AnyAsync(x => x.MaNhom == id)) return Forbid();

            var list = await _db.ThanhVienNhoms
                .Where(x => x.MaNhom == id && x.ThanhVien.TrangThai)
                .Select(x => new { maThanhVien = x.MaThanhVien, hoTen = x.ThanhVien.NguoiDung.HoTen })
                .OrderBy(x => x.hoTen)
                .ToListAsync();
            return Json(list);
        }

        // ----- Hàm phụ -----
        private async Task LoadMembers()
        {
            var members = await _db.ThanhVienDoanhNghieps.Include(x => x.NguoiDung)
                .Where(x => x.TrangThai)
                .OrderBy(x => x.NguoiDung.HoTen)
                .ToListAsync();
            ViewBag.Members = members;
            ViewBag.MemberSelect = new SelectList(members.Select(x => new { x.MaThanhVien, Ten = x.NguoiDung.HoTen }), "MaThanhVien", "Ten");
        }

        // Chỉ chấp nhận mã thành viên thuộc doanh nghiệp hiện tại
        private async Task KiemTraThanhVien(GroupFormViewModel model)
        {
            model.MaThanhViens ??= new List<int>();
            var hopLe = await _db.ThanhVienDoanhNghieps.Where(x => x.TrangThai).Select(x => x.MaThanhVien).ToListAsync();

            if (model.MaTruongNhom != null && !hopLe.Contains(model.MaTruongNhom.Value))
                ModelState.AddModelError(nameof(model.MaTruongNhom), "Trưởng nhóm không hợp lệ.");
            if (model.MaThanhViens.Any(x => !hopLe.Contains(x)))
                ModelState.AddModelError(nameof(model.MaThanhViens), "Danh sách thành viên không hợp lệ.");
        }

        // Đồng bộ danh sách thành viên nhóm (trưởng nhóm luôn là thành viên)
        private async Task CapNhatThanhVien(NhomLamViec nhom, List<int> maThanhViens)
        {
            var moi = maThanhViens.ToList();
            if (nhom.MaTruongNhom != null && !moi.Contains(nhom.MaTruongNhom.Value))
                moi.Add(nhom.MaTruongNhom.Value);

            var hienTai = await _db.ThanhVienNhoms.Where(x => x.MaNhom == nhom.MaNhom).ToListAsync();

            _db.ThanhVienNhoms.RemoveRange(hienTai.Where(x => !moi.Contains(x.MaThanhVien)));

            foreach (var maTV in moi.Where(x => !hienTai.Any(h => h.MaThanhVien == x)))
            {
                _db.ThanhVienNhoms.Add(new ThanhVienNhom { MaNhom = nhom.MaNhom, MaThanhVien = maTV });
                var tv = await _db.ThanhVienDoanhNghieps.FindAsync(maTV);
                _notify.Gui(tv.MaNguoiDung, "Bạn được thêm vào nhóm " + nhom.TenNhom, "/Group/Details/" + nhom.MaNhom);
            }
        }
    }
}
