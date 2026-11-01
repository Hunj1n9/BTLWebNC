using System.ComponentModel.DataAnnotations;

namespace WorkHub.Models
{
    // Bảng NguoiDung: tài khoản cá nhân
    public class NguoiDung
    {
        [Key]
        public int MaNguoiDung { get; set; }

        [Required, StringLength(100)]
        public string HoTen { get; set; }

        [Required, StringLength(100)]
        public string Email { get; set; }

        [Required]
        public string MatKhauHash { get; set; }

        // true = Hoạt động, false = Bị khóa
        public bool TrangThai { get; set; } = true;

        // Đánh dấu tài khoản quản trị hệ thống (Super Admin)
        public bool LaSuperAdmin { get; set; }

        public DateTime NgayTao { get; set; } = DateTime.Now;

        public List<ThanhVienDoanhNghiep> ThanhViens { get; set; } = new();
    }
}
