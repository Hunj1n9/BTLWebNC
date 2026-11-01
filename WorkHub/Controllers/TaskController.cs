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
    public class TaskController : Controller
    {
        private readonly AppDbContext _db;
        private readonly CurrentUser _me;
        private readonly NotificationService _notify;

        public TaskController(AppDbContext db, CurrentUser me, NotificationService notify)
        {
            _db = db;
            _me = me;
            _notify = notify;
        }

        // ===== Danh sách công việc =====
        public async Task<IActionResult> Index(TaskFilterViewModel filter)
        {
            ViewBag.Members = new SelectList(await _db.ThanhVienDoanhNghieps.Include(x => x.NguoiDung)
                .Select(x => new { x.MaThanhVien, Ten = x.NguoiDung.HoTen }).ToListAsync(), "MaThanhVien", "Ten");
            ViewBag.Tasks = await TimKiem(filter);
            return View(filter);
        }

        // ===== AJAX: tìm kiếm, lọc công việc (R13, BM10) =====
        [HttpGet]
        public async Task<IActionResult> Search(TaskFilterViewModel filter)
        {
            return PartialView("_TaskList", await TimKiem(filter));
        }

        private Task<List<CongViec>> TimKiem(TaskFilterViewModel f)
        {
            var query = QuyenHelper.CongViecDuocXem(_db, _me)
                .Include(x => x.NhomLamViec)
                .Include(x => x.NguoiThucHien).ThenInclude(x => x.NguoiDung)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(f.TuKhoa)) query = query.Where(x => x.TenCongViec.Contains(f.TuKhoa));
            if (!string.IsNullOrEmpty(f.TrangThai)) query = query.Where(x => x.TrangThai == f.TrangThai);
            if (f.HanTu != null) query = query.Where(x => x.HanHoanThanh >= f.HanTu);
            if (f.HanDen != null) query = query.Where(x => x.HanHoanThanh <= f.HanDen);
            if (f.MaNguoiThucHien != null) query = query.Where(x => x.MaNguoiThucHien == f.MaNguoiThucHien);
            if (f.ChiCuaToi)
            {
                int? memberId = _me.MemberId;
                query = query.Where(x => x.MaNguoiThucHien == memberId);
            }

            return query.OrderBy(x => x.TrangThai == TrangThaiCV.HoanThanh).ThenBy(x => x.HanHoanThanh).ToListAsync();
        }

        // ===== Chi tiết công việc + bình luận =====
        public async Task<IActionResult> Details(int id)
        {
            var cv = await QuyenHelper.CongViecDuocXem(_db, _me)
                .Include(x => x.NhomLamViec)
                .Include(x => x.NguoiGiao).ThenInclude(x => x.NguoiDung)
                .Include(x => x.NguoiThucHien).ThenInclude(x => x.NguoiDung)
                .FirstOrDefaultAsync(x => x.MaCongViec == id);
            if (cv == null) return NotFound();

            ViewBag.CoTheSua = QuyenHelper.CoTheGiaoViec(_me, cv.NhomLamViec);
            ViewBag.CoTheXoa = _me.IsTenantAdmin || cv.MaNguoiGiao == _me.MemberId;
            ViewBag.CoTheDoiTrangThai = _me.IsTenantAdmin || cv.MaNguoiThucHien == _me.MemberId;
            return View(cv);
        }

        // ===== Tạo công việc (BM06, R01) =====
        [HttpGet]
        public async Task<IActionResult> Create(int? maNhom)
        {
            var nhoms = await NhomDuocGiaoViec();
            if (!nhoms.Any())
            {
                TempData["Error"] = "Bạn không có quyền giao việc ở nhóm nào.";
                return RedirectToAction(nameof(Index));
            }
            var model = new TaskFormViewModel { MaNhom = maNhom };
            await LoadForm(model, nhoms);
            return View("Form", model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(TaskFormViewModel model)
        {
            var nhoms = await NhomDuocGiaoViec();
            await KiemTra(model, nhoms);
            if (!ModelState.IsValid)
            {
                await LoadForm(model, nhoms);
                return View("Form", model);
            }

            var cv = new CongViec
            {
                TenCongViec = model.TenCongViec.Trim(),
                MoTa = model.MoTa,
                MaNhom = model.MaNhom.Value,
                MaNguoiThucHien = model.MaNguoiThucHien.Value,
                MaNguoiGiao = _me.MemberId.Value,
                NgayBatDau = model.NgayBatDau,
                HanHoanThanh = model.HanHoanThanh,
                TrangThai = model.TrangThai,
                DoUuTien = model.DoUuTien
            };
            _db.CongViecs.Add(cv);
            await _db.SaveChangesAsync();

            // Thông báo cho người được giao (R11)
            var nguoiNhan = await _db.ThanhVienDoanhNghieps.FindAsync(cv.MaNguoiThucHien);
            _notify.Gui(nguoiNhan.MaNguoiDung, _me.HoTen + " đã giao cho bạn công việc: " + cv.TenCongViec, "/Task/Details/" + cv.MaCongViec);
            await _db.SaveChangesAsync();

            TempData["Message"] = "Đã tạo công việc.";
            return RedirectToAction(nameof(Details), new { id = cv.MaCongViec });
        }

        // ===== Sửa công việc (R03) =====
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var cv = await _db.CongViecs.Include(x => x.NhomLamViec).FirstOrDefaultAsync(x => x.MaCongViec == id);
            if (cv == null) return NotFound();
            if (!QuyenHelper.CoTheGiaoViec(_me, cv.NhomLamViec)) return Forbid();

            var model = new TaskFormViewModel
            {
                MaCongViec = cv.MaCongViec,
                TenCongViec = cv.TenCongViec,
                MoTa = cv.MoTa,
                MaNhom = cv.MaNhom,
                MaNguoiThucHien = cv.MaNguoiThucHien,
                NgayBatDau = cv.NgayBatDau,
                HanHoanThanh = cv.HanHoanThanh,
                TrangThai = cv.TrangThai,
                DoUuTien = cv.DoUuTien
            };
            await LoadForm(model, await NhomDuocGiaoViec());
            return View("Form", model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, TaskFormViewModel model)
        {
            var cv = await _db.CongViecs.Include(x => x.NhomLamViec).FirstOrDefaultAsync(x => x.MaCongViec == id);
            if (cv == null) return NotFound();
            if (!QuyenHelper.CoTheGiaoViec(_me, cv.NhomLamViec)) return Forbid();

            var nhoms = await NhomDuocGiaoViec();
            await KiemTra(model, nhoms);
            if (!ModelState.IsValid)
            {
                model.MaCongViec = id;
                await LoadForm(model, nhoms);
                return View("Form", model);
            }

            bool doiNguoi = cv.MaNguoiThucHien != model.MaNguoiThucHien;

            cv.TenCongViec = model.TenCongViec.Trim();
            cv.MoTa = model.MoTa;
            cv.MaNhom = model.MaNhom.Value;
            cv.MaNguoiThucHien = model.MaNguoiThucHien.Value;
            cv.NgayBatDau = model.NgayBatDau;
            cv.HanHoanThanh = model.HanHoanThanh;
            cv.TrangThai = model.TrangThai;
            cv.DoUuTien = model.DoUuTien;

            var nguoiNhan = await _db.ThanhVienDoanhNghieps.FindAsync(cv.MaNguoiThucHien);
            string noiDung = doiNguoi
                ? _me.HoTen + " đã giao cho bạn công việc: " + cv.TenCongViec
                : _me.HoTen + " đã cập nhật công việc: " + cv.TenCongViec;
            _notify.Gui(nguoiNhan.MaNguoiDung, noiDung, "/Task/Details/" + cv.MaCongViec);
            await _db.SaveChangesAsync();

            TempData["Message"] = "Đã cập nhật công việc.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // ===== Xóa / thu hồi công việc (R03) =====
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var cv = await _db.CongViecs.FirstOrDefaultAsync(x => x.MaCongViec == id);
            if (cv == null) return NotFound();
            if (!_me.IsTenantAdmin && cv.MaNguoiGiao != _me.MemberId) return Forbid();

            // Xóa bình luận của công việc trước (không dùng cascade)
            var binhLuans = await _db.TraoDoiBinhLuans.IgnoreQueryFilters().Where(x => x.MaCongViec == id).ToListAsync();
            _db.TraoDoiBinhLuans.RemoveRange(binhLuans);
            _db.CongViecs.Remove(cv);
            await _db.SaveChangesAsync();

            TempData["Message"] = "Đã xóa công việc.";
            return RedirectToAction(nameof(Index));
        }

        // ===== AJAX: cập nhật trạng thái (R02) =====
        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int maCongViec, string trangThai)
        {
            if (!TrangThaiCV.TatCa.Contains(trangThai))
                return Json(new { success = false, message = "Trạng thái không hợp lệ." });

            var cv = await _db.CongViecs.Include(x => x.NguoiGiao).Include(x => x.NguoiThucHien)
                .FirstOrDefaultAsync(x => x.MaCongViec == maCongViec);
            if (cv == null)
                return Json(new { success = false, message = "Không tìm thấy công việc." });

            // Chỉ người thực hiện hoặc TenantAdmin được đổi trạng thái
            if (!_me.IsTenantAdmin && cv.MaNguoiThucHien != _me.MemberId)
                return Json(new { success = false, message = "Bạn không có quyền cập nhật công việc này." });

            cv.TrangThai = trangThai;

            string noiDung = _me.HoTen + " đã chuyển \"" + cv.TenCongViec + "\" sang " + trangThai;
            string link = "/Task/Details/" + cv.MaCongViec;
            _notify.Gui(cv.NguoiGiao.MaNguoiDung, noiDung, link);
            if (cv.NguoiThucHien.MaNguoiDung != cv.NguoiGiao.MaNguoiDung)
                _notify.Gui(cv.NguoiThucHien.MaNguoiDung, noiDung, link);

            await _db.SaveChangesAsync();
            return Json(new { success = true, trangThai = cv.TrangThai });
        }

        // ----- Hàm phụ -----

        // Nhóm (đang mở) mà người dùng được phép giao việc
        private Task<List<NhomLamViec>> NhomDuocGiaoViec()
        {
            var query = _db.NhomLamViecs.Where(x => !x.DaDong);
            if (!_me.IsTenantAdmin)
            {
                int? memberId = _me.MemberId;
                query = query.Where(x => x.MaTruongNhom == memberId);
            }
            return query.OrderBy(x => x.TenNhom).ToListAsync();
        }

        private async Task KiemTra(TaskFormViewModel model, List<NhomLamViec> nhoms)
        {
            if (model.MaNhom != null && !nhoms.Any(x => x.MaNhom == model.MaNhom))
                ModelState.AddModelError(nameof(model.MaNhom), "Bạn không có quyền giao việc trong nhóm này.");

            if (model.MaNhom != null && model.MaNguoiThucHien != null)
            {
                bool trongNhom = await _db.ThanhVienNhoms.AnyAsync(x => x.MaNhom == model.MaNhom
                                                                     && x.MaThanhVien == model.MaNguoiThucHien
                                                                     && x.ThanhVien.TrangThai);
                if (!trongNhom)
                    ModelState.AddModelError(nameof(model.MaNguoiThucHien), "Người thực hiện phải là thành viên của nhóm.");
            }
        }

        private async Task LoadForm(TaskFormViewModel model, List<NhomLamViec> nhoms)
        {
            ViewBag.Nhoms = new SelectList(nhoms, "MaNhom", "TenNhom", model.MaNhom);

            var members = model.MaNhom == null
                ? new List<SelectListItem>()
                : await _db.ThanhVienNhoms
                    .Where(x => x.MaNhom == model.MaNhom && x.ThanhVien.TrangThai)
                    .Select(x => new SelectListItem { Value = x.MaThanhVien.ToString(), Text = x.ThanhVien.NguoiDung.HoTen })
                    .ToListAsync();
            ViewBag.NguoiThucHiens = members;
        }
    }
}
