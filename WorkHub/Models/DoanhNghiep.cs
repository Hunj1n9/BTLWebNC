using System.ComponentModel.DataAnnotations;

namespace WorkHub.Models
{
    // Bảng DoanhNghiep (Tenant)
    public class DoanhNghiep
    {
        [Key]
        public int MaDoanhNghiep { get; set; }

        [Required, StringLength(200)]
        public string TenDoanhNghiep { get; set; }

        [StringLength(100)]
        public string Email { get; set; }

        [StringLength(20)]
        public string SoDienThoai { get; set; }

        [StringLength(300)]
        public string DiaChi { get; set; }

        [StringLength(500)]
        public string LogoUrl { get; set; }

        public string MoTa { get; set; }

        // true = Hoạt động, false = Bị khóa bởi Super Admin
        public bool TrangThai { get; set; } = true;

        public DateTime NgayTao { get; set; } = DateTime.Now;

        public List<ThanhVienDoanhNghiep> ThanhViens { get; set; } = new();
        public List<NhomLamViec> Nhoms { get; set; } = new();
    }
}
