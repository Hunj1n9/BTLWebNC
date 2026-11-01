using WorkHub.Data;
using WorkHub.Models;

namespace WorkHub.Services
{
    // Tạo thông báo khi có sự kiện (giao việc, đổi trạng thái, bình luận, nhắc tên...)
    // Chỉ thêm vào DbContext, Controller gọi SaveChanges sau đó.
    public class NotificationService
    {
        private readonly AppDbContext _db;
        private readonly CurrentUser _me;

        public NotificationService(AppDbContext db, CurrentUser me)
        {
            _db = db;
            _me = me;
        }

        public void Gui(int maNguoiNhan, string noiDung, string lienKet)
        {
            // Không tự gửi thông báo cho chính mình
            if (maNguoiNhan == _me.UserId) return;

            _db.ThongBaos.Add(new ThongBao
            {
                MaNguoiNhan = maNguoiNhan,
                NoiDung = noiDung.Length > 500 ? noiDung.Substring(0, 500) : noiDung,
                LienKet = lienKet,
                ThoiGian = DateTime.Now
            });
        }
    }
}
