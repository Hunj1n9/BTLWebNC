using WorkHub.Models;
using WorkHub.Services;

namespace WorkHub.Data
{
    // Tạo dữ liệu mẫu để demo. Tài khoản mẫu được liệt kê trong README.md
    public static class SeedData
    {
        public static void Initialize(AppDbContext db)
        {
            db.Database.EnsureCreated();

            if (db.NguoiDungs.Any()) return; // đã có dữ liệu

            var matKhau = AuthService.HashPassword("123456");

            var admin = new NguoiDung { HoTen = "Quản trị hệ thống", Email = "admin@workhub.vn", MatKhauHash = matKhau, LaSuperAdmin = true };
            var giamDoc = new NguoiDung { HoTen = "Dương Quốc Khánh", Email = "khanh@abc.vn", MatKhauHash = matKhau };
            var nv1 = new NguoiDung { HoTen = "Nguyễn Tiến Huy", Email = "huy@abc.vn", MatKhauHash = matKhau };
            var nv2 = new NguoiDung { HoTen = "Nguyễn Phúc Hưng", Email = "hung@abc.vn", MatKhauHash = matKhau };
            var khac = new NguoiDung { HoTen = "Trần Văn Nam", Email = "nam@xyz.vn", MatKhauHash = matKhau };
            db.NguoiDungs.AddRange(admin, giamDoc, nv1, nv2, khac);

            var abc = new DoanhNghiep { TenDoanhNghiep = "Công ty ABC", Email = "lienhe@abc.vn", SoDienThoai = "0241234567", DiaChi = "Hà Nội", MoTa = "Công ty phần mềm ABC" };
            var xyz = new DoanhNghiep { TenDoanhNghiep = "Công ty XYZ", Email = "lienhe@xyz.vn", DiaChi = "TP. Hồ Chí Minh" };
            db.DoanhNghieps.AddRange(abc, xyz);

            var tvKhanh = new ThanhVienDoanhNghiep { NguoiDung = giamDoc, DoanhNghiep = abc, VaiTro = VaiTros.TenantAdmin };
            var tvHuy = new ThanhVienDoanhNghiep { NguoiDung = nv1, DoanhNghiep = abc, VaiTro = VaiTros.Member };
            var tvHung = new ThanhVienDoanhNghiep { NguoiDung = nv2, DoanhNghiep = abc, VaiTro = VaiTros.Member };
            var tvNam = new ThanhVienDoanhNghiep { NguoiDung = khac, DoanhNghiep = xyz, VaiTro = VaiTros.TenantAdmin };
            db.ThanhVienDoanhNghieps.AddRange(tvKhanh, tvHuy, tvHung, tvNam);

            var nhomWeb = new NhomLamViec { TenNhom = "Nhóm Web", MoTa = "Phát triển website", DoanhNghiep = abc, TruongNhom = tvHuy };
            var nhomTest = new NhomLamViec { TenNhom = "Nhóm Kiểm thử", MoTa = "Kiểm thử phần mềm", DoanhNghiep = abc, TruongNhom = tvHung };
            db.NhomLamViecs.AddRange(nhomWeb, nhomTest);

            db.ThanhVienNhoms.AddRange(
                new ThanhVienNhom { NhomLamViec = nhomWeb, ThanhVien = tvHuy },
                new ThanhVienNhom { NhomLamViec = nhomWeb, ThanhVien = tvHung },
                new ThanhVienNhom { NhomLamViec = nhomTest, ThanhVien = tvHung });

            var cv1 = new CongViec { TenCongViec = "Thiết kế giao diện trang chủ", MoTa = "Làm HTML/CSS cho trang chủ", NguoiGiao = tvKhanh, NguoiThucHien = tvHuy, NhomLamViec = nhomWeb, TrangThai = TrangThaiCV.DangLam, DoUuTien = "Cao" };
            var cv2 = new CongViec { TenCongViec = "Viết API đăng nhập", NguoiGiao = tvKhanh, NguoiThucHien = tvHung, NhomLamViec = nhomWeb, TrangThai = TrangThaiCV.ChuaLam };
            var cv3 = new CongViec { TenCongViec = "Viết test case đăng ký", NguoiGiao = tvKhanh, NguoiThucHien = tvHung, NhomLamViec = nhomTest, TrangThai = TrangThaiCV.HoanThanh, DoUuTien = "Thấp", NgayBatDau = DateTime.Today.AddDays(-10), HanHoanThanh = DateTime.Today.AddDays(-2) };
            db.CongViecs.AddRange(cv1, cv2, cv3);

            db.TraoDoiBinhLuans.AddRange(
                new TraoDoiBinhLuan { NguoiGui = tvKhanh, CongViec = cv1, NoiDung = "Nhớ làm responsive cho điện thoại nhé." },
                new TraoDoiBinhLuan { NguoiGui = tvHuy, NhomLamViec = nhomWeb, NoiDung = "Chào cả nhóm, chiều nay họp lúc 3h." });

            db.SaveChanges();

            db.ThongBaos.Add(new ThongBao { MaNguoiNhan = nv1.MaNguoiDung, NoiDung = "Bạn được giao công việc: Thiết kế giao diện trang chủ", LienKet = "/Task/Details/" + cv1.MaCongViec });
            db.SaveChanges();
        }
    }
}
