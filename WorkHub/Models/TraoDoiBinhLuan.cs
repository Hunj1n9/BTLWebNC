using System.ComponentModel.DataAnnotations;

namespace WorkHub.Models
{
    // Bảng TraoDoiBinhLuan: tin nhắn trong nhóm hoặc bình luận dưới công việc
    public class TraoDoiBinhLuan
    {
        [Key]
        public int MaBinhLuan { get; set; }

        public int MaNguoiGui { get; set; }
        public ThanhVienDoanhNghiep NguoiGui { get; set; }

        [Required, StringLength(2000)]
        public string NoiDung { get; set; }

        public DateTime ThoiGian { get; set; } = DateTime.Now;

        // Một trong hai khóa dưới đây phải có giá trị (ràng buộc CHECK)
        public int? MaCongViec { get; set; }
        public CongViec CongViec { get; set; }

        public int? MaNhom { get; set; }
        public NhomLamViec NhomLamViec { get; set; }
    }
}
