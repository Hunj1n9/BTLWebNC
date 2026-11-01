using WorkHub.Data;
using WorkHub.Models;

namespace WorkHub.Services
{
    // Các quy tắc phân quyền dùng chung.
    // Lưu ý: dữ liệu đã được Global Query Filter giới hạn theo doanh nghiệp,
    // các hàm dưới đây giới hạn thêm theo nhóm/công việc cho vai trò Member.
    public static class QuyenHelper
    {
        // Nhóm mà người dùng được xem: TenantAdmin xem tất cả, Member chỉ xem nhóm mình tham gia
        public static IQueryable<NhomLamViec> NhomDuocXem(AppDbContext db, CurrentUser me)
        {
            if (me.IsTenantAdmin) return db.NhomLamViecs;
            int? memberId = me.MemberId;
            return db.NhomLamViecs.Where(n => n.MaTruongNhom == memberId
                                           || n.ThanhViens.Any(t => t.MaThanhVien == memberId));
        }

        // Công việc mà người dùng được xem: thuộc nhóm mình tham gia hoặc được giao cho mình
        public static IQueryable<CongViec> CongViecDuocXem(AppDbContext db, CurrentUser me)
        {
            if (me.IsTenantAdmin) return db.CongViecs;
            int? memberId = me.MemberId;
            return db.CongViecs.Where(c => c.MaNguoiThucHien == memberId
                                        || c.NhomLamViec.MaTruongNhom == memberId
                                        || c.NhomLamViec.ThanhViens.Any(t => t.MaThanhVien == memberId));
        }

        // Được tạo / sửa công việc trong nhóm: TenantAdmin hoặc Trưởng nhóm
        public static bool CoTheGiaoViec(CurrentUser me, NhomLamViec nhom)
        {
            return me.IsTenantAdmin || nhom.MaTruongNhom == me.MemberId;
        }
    }
}
