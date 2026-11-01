namespace WorkHub.Models
{
    // Bảng trung gian ThanhVienNhom (khóa chính kép MaNhom + MaThanhVien)
    public class ThanhVienNhom
    {
        public int MaNhom { get; set; }
        public NhomLamViec NhomLamViec { get; set; }

        public int MaThanhVien { get; set; }
        public ThanhVienDoanhNghiep ThanhVien { get; set; }
    }
}
