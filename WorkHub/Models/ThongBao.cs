using System.ComponentModel.DataAnnotations;

namespace WorkHub.Models
{
    // Bảng ThongBao
    public class ThongBao
    {
        [Key]
        public int MaThongBao { get; set; }

        public int MaNguoiNhan { get; set; }
        public NguoiDung NguoiNhan { get; set; }

        [Required, StringLength(500)]
        public string NoiDung { get; set; }

        [StringLength(255)]
        public string LienKet { get; set; }

        public DateTime ThoiGian { get; set; } = DateTime.Now;

        public bool TrangThaiDaDoc { get; set; }
    }
}
