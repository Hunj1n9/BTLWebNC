using System.ComponentModel.DataAnnotations;

namespace WorkHub.Models
{
    // Bảng NhomLamViec: nhóm / phòng ban trong doanh nghiệp
    public class NhomLamViec
    {
        [Key]
        public int MaNhom { get; set; }

        [Required, StringLength(150)]
        public string TenNhom { get; set; }

        public string MoTa { get; set; }

        public int MaDoanhNghiep { get; set; }
        public DoanhNghiep DoanhNghiep { get; set; }

        // Trưởng nhóm (mã thành viên doanh nghiệp)
        public int? MaTruongNhom { get; set; }
        public ThanhVienDoanhNghiep TruongNhom { get; set; }

        // Nhóm đã đóng (archive) hay chưa
        public bool DaDong { get; set; }

        public List<ThanhVienNhom> ThanhViens { get; set; } = new();
        public List<CongViec> CongViecs { get; set; } = new();
    }
}
