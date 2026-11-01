using System.ComponentModel.DataAnnotations;

namespace WorkHub.Models
{
    // Bảng ThanhVienDoanhNghiep: người dùng thuộc doanh nghiệp nào, vai trò gì
    public class ThanhVienDoanhNghiep
    {
        [Key]
        public int MaThanhVien { get; set; }

        public int MaNguoiDung { get; set; }
        public NguoiDung NguoiDung { get; set; }

        public int MaDoanhNghiep { get; set; }
        public DoanhNghiep DoanhNghiep { get; set; }

        // "TenantAdmin" hoặc "Member"
        [Required, StringLength(50)]
        public string VaiTro { get; set; } = VaiTros.Member;

        // true = Hoạt động, false = Vô hiệu hóa
        public bool TrangThai { get; set; } = true;

        public List<ThanhVienNhom> ThanhVienNhoms { get; set; } = new();
    }

    // Tên các vai trò dùng trong hệ thống
    public static class VaiTros
    {
        public const string SuperAdmin = "SuperAdmin";
        public const string TenantAdmin = "TenantAdmin";
        public const string Member = "Member";
    }
}
