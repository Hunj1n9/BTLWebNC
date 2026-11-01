using WorkHub.Models;

namespace WorkHub.Services
{
    // Hàm nhỏ dùng trong View
    public static class ViewHelper
    {
        // Màu badge theo trạng thái công việc
        public static string StatusClass(string trangThai) => trangThai switch
        {
            TrangThaiCV.DangLam => "pill status-danglam",
            TrangThaiCV.HoanThanh => "pill status-hoanthanh",
            _ => "pill status-chualam"
        };

        public static string PriorityClass(string doUuTien) => doUuTien switch
        {
            "Cao" => "prio prio-cao",
            "Thấp" => "prio prio-thap",
            _ => "prio prio-tb"
        };

        public static string RoleName(string vaiTro) => vaiTro == VaiTros.TenantAdmin ? "Quản trị" : "Thành viên";

        public static bool QuaHan(CongViec cv) => cv.TrangThai != TrangThaiCV.HoanThanh && cv.HanHoanThanh < DateTime.Today;
    }
}
