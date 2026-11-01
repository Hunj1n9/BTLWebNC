using System.ComponentModel.DataAnnotations;

namespace WorkHub.Models.ViewModels
{
    // BM03. Thông tin doanh nghiệp
    public class TenantViewModel
    {
        public int MaDoanhNghiep { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên doanh nghiệp")]
        [StringLength(200)]
        [Display(Name = "Tên doanh nghiệp")]
        public string TenDoanhNghiep { get; set; }

        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [StringLength(100)]
        public string Email { get; set; }

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [StringLength(20)]
        [Display(Name = "Số điện thoại")]
        public string SoDienThoai { get; set; }

        [StringLength(300)]
        [Display(Name = "Địa chỉ")]
        public string DiaChi { get; set; }

        [StringLength(2000)]
        [Display(Name = "Mô tả")]
        public string MoTa { get; set; }

        public string LogoUrl { get; set; }

        [Display(Name = "Logo (JPG/PNG, tối đa 2MB)")]
        public IFormFile Logo { get; set; }
    }

    // BM04. Mời / thêm thành viên
    public class InviteViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; }

        // Chỉ cần khi email chưa có tài khoản
        [StringLength(100)]
        [Display(Name = "Họ tên")]
        public string HoTen { get; set; }

        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")]
        [Display(Name = "Mật khẩu tạm")]
        public string MatKhauTam { get; set; }

        [Display(Name = "Vai trò")]
        public string VaiTro { get; set; } = VaiTros.Member;
    }

    // BM05. Tạo / sửa nhóm làm việc
    public class GroupFormViewModel
    {
        public int MaNhom { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên nhóm")]
        [StringLength(150)]
        [Display(Name = "Tên nhóm")]
        public string TenNhom { get; set; }

        [StringLength(2000)]
        [Display(Name = "Mô tả")]
        public string MoTa { get; set; }

        [Display(Name = "Trưởng nhóm")]
        public int? MaTruongNhom { get; set; }

        [Display(Name = "Thành viên")]
        public List<int> MaThanhViens { get; set; } = new();

        [Display(Name = "Đóng nhóm (lưu trữ)")]
        public bool DaDong { get; set; }
    }

    // BM06. Tạo / sửa công việc
    public class TaskFormViewModel : IValidatableObject
    {
        public int MaCongViec { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên công việc")]
        [StringLength(200)]
        [Display(Name = "Tên công việc")]
        public string TenCongViec { get; set; }

        [StringLength(4000)]
        [Display(Name = "Mô tả")]
        public string MoTa { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn nhóm")]
        [Display(Name = "Nhóm")]
        public int? MaNhom { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn người thực hiện")]
        [Display(Name = "Người thực hiện")]
        public int? MaNguoiThucHien { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Ngày bắt đầu")]
        public DateTime NgayBatDau { get; set; } = DateTime.Today;

        [DataType(DataType.Date)]
        [Display(Name = "Hạn hoàn thành")]
        public DateTime HanHoanThanh { get; set; } = DateTime.Today.AddDays(7);

        [Display(Name = "Trạng thái")]
        public string TrangThai { get; set; } = TrangThaiCV.ChuaLam;

        [Display(Name = "Độ ưu tiên")]
        public string DoUuTien { get; set; } = "Trung bình";

        public IEnumerable<ValidationResult> Validate(ValidationContext context)
        {
            if (HanHoanThanh < NgayBatDau)
                yield return new ValidationResult("Hạn hoàn thành phải sau ngày bắt đầu", new[] { nameof(HanHoanThanh) });
            if (!TrangThaiCV.TatCa.Contains(TrangThai))
                yield return new ValidationResult("Trạng thái không hợp lệ", new[] { nameof(TrangThai) });
            if (!TrangThaiCV.DoUuTiens.Contains(DoUuTien))
                yield return new ValidationResult("Độ ưu tiên không hợp lệ", new[] { nameof(DoUuTien) });
        }
    }

    // BM10. Bộ lọc công việc
    public class TaskFilterViewModel
    {
        public string TuKhoa { get; set; }
        public string TrangThai { get; set; }
        public DateTime? HanTu { get; set; }
        public DateTime? HanDen { get; set; }
        public int? MaNguoiThucHien { get; set; }
        public bool ChiCuaToi { get; set; }
    }

    // Số liệu cho trang Dashboard doanh nghiệp
    public class DashboardViewModel
    {
        public string TenDoanhNghiep { get; set; }
        public int SoThanhVien { get; set; }
        public int SoNhom { get; set; }
        public int SoChuaLam { get; set; }
        public int SoDangLam { get; set; }
        public int SoHoanThanh { get; set; }
        public int SoQuaHan { get; set; }
        public int TongCongViec => SoChuaLam + SoDangLam + SoHoanThanh;
        public List<CongViec> CongViecCuaToi { get; set; } = new();
        public List<TienDoNhom> TienDoNhoms { get; set; } = new();
    }

    // Tiến độ của một nhóm: số việc hoàn thành / tổng số việc
    public class TienDoNhom
    {
        public int MaNhom { get; set; }
        public string TenNhom { get; set; }
        public int Tong { get; set; }
        public int HoanThanh { get; set; }
        public int PhanTram => Tong == 0 ? 0 : HoanThanh * 100 / Tong;
    }

    // Số liệu cho trang Admin
    public class AdminDashboardViewModel
    {
        public int SoNguoiDung { get; set; }
        public int SoDoanhNghiep { get; set; }
        public int SoDoanhNghiepBiKhoa { get; set; }
        public int SoNhom { get; set; }
        public int SoCongViec { get; set; }
        public List<DoanhNghiep> DoanhNghiepMoi { get; set; } = new();
    }
}
