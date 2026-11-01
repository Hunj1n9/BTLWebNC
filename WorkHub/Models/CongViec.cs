using System.ComponentModel.DataAnnotations;

namespace WorkHub.Models
{
    // Bảng CongViec
    public class CongViec
    {
        [Key]
        public int MaCongViec { get; set; }

        [Required, StringLength(200)]
        public string TenCongViec { get; set; }

        public string MoTa { get; set; }

        public int MaNguoiGiao { get; set; }
        public ThanhVienDoanhNghiep NguoiGiao { get; set; }

        public int MaNguoiThucHien { get; set; }
        public ThanhVienDoanhNghiep NguoiThucHien { get; set; }

        public int MaNhom { get; set; }
        public NhomLamViec NhomLamViec { get; set; }

        public DateTime NgayBatDau { get; set; } = DateTime.Today;
        public DateTime HanHoanThanh { get; set; } = DateTime.Today.AddDays(7);

        [Required, StringLength(50)]
        public string TrangThai { get; set; } = TrangThaiCV.ChuaLam;

        [Required, StringLength(50)]
        public string DoUuTien { get; set; } = "Trung bình";

        public List<TraoDoiBinhLuan> BinhLuans { get; set; } = new();
    }

    public static class TrangThaiCV
    {
        public const string ChuaLam = "Chưa làm";
        public const string DangLam = "Đang làm";
        public const string HoanThanh = "Hoàn thành";

        public static readonly string[] TatCa = { ChuaLam, DangLam, HoanThanh };
        public static readonly string[] DoUuTiens = { "Thấp", "Trung bình", "Cao" };
    }
}
