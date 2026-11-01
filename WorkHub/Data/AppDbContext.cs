using Microsoft.EntityFrameworkCore;
using WorkHub.Models;
using WorkHub.Services;

namespace WorkHub.Data
{
    public class AppDbContext : DbContext
    {
        private readonly CurrentUser _currentUser;

        public AppDbContext(DbContextOptions<AppDbContext> options, CurrentUser currentUser) : base(options)
        {
            _currentUser = currentUser;
        }

        // Mã doanh nghiệp của người đang đăng nhập (lấy từ Claims, không lấy từ URL/form)
        public int? TenantId => _currentUser?.TenantId;

        public DbSet<NguoiDung> NguoiDungs { get; set; }
        public DbSet<DoanhNghiep> DoanhNghieps { get; set; }
        public DbSet<ThanhVienDoanhNghiep> ThanhVienDoanhNghieps { get; set; }
        public DbSet<NhomLamViec> NhomLamViecs { get; set; }
        public DbSet<ThanhVienNhom> ThanhVienNhoms { get; set; }
        public DbSet<CongViec> CongViecs { get; set; }
        public DbSet<TraoDoiBinhLuan> TraoDoiBinhLuans { get; set; }
        public DbSet<ThongBao> ThongBaos { get; set; }

        protected override void OnModelCreating(ModelBuilder mb)
        {
            // ----- Tên bảng -----
            mb.Entity<NguoiDung>().ToTable("NguoiDung");
            mb.Entity<DoanhNghiep>().ToTable("DoanhNghiep");
            mb.Entity<ThanhVienDoanhNghiep>().ToTable("ThanhVienDoanhNghiep");
            mb.Entity<NhomLamViec>().ToTable("NhomLamViec");
            mb.Entity<ThanhVienNhom>().ToTable("ThanhVienNhom");
            mb.Entity<CongViec>().ToTable("CongViec");
            mb.Entity<ThongBao>().ToTable("ThongBao");
            mb.Entity<TraoDoiBinhLuan>().ToTable("TraoDoiBinhLuan", t =>
                t.HasCheckConstraint("CK_TraoDoiBinhLuan_DoiTuong", "[MaCongViec] IS NOT NULL OR [MaNhom] IS NOT NULL"));

            // ----- Ràng buộc duy nhất & chỉ mục -----
            mb.Entity<NguoiDung>().HasIndex(x => x.Email).IsUnique();
            mb.Entity<ThanhVienDoanhNghiep>().HasIndex(x => new { x.MaNguoiDung, x.MaDoanhNghiep }).IsUnique();
            mb.Entity<ThanhVienNhom>().HasKey(x => new { x.MaNhom, x.MaThanhVien });
            mb.Entity<NhomLamViec>().HasIndex(x => x.MaDoanhNghiep);
            mb.Entity<CongViec>().HasIndex(x => new { x.MaNguoiThucHien, x.TrangThai });
            mb.Entity<ThongBao>().HasIndex(x => new { x.MaNguoiNhan, x.TrangThaiDaDoc });

            // ----- Quan hệ -----
            mb.Entity<ThanhVienDoanhNghiep>()
                .HasOne(x => x.NguoiDung).WithMany(x => x.ThanhViens).HasForeignKey(x => x.MaNguoiDung);
            mb.Entity<ThanhVienDoanhNghiep>()
                .HasOne(x => x.DoanhNghiep).WithMany(x => x.ThanhViens).HasForeignKey(x => x.MaDoanhNghiep);

            mb.Entity<NhomLamViec>()
                .HasOne(x => x.DoanhNghiep).WithMany(x => x.Nhoms).HasForeignKey(x => x.MaDoanhNghiep);
            mb.Entity<NhomLamViec>()
                .HasOne(x => x.TruongNhom).WithMany().HasForeignKey(x => x.MaTruongNhom);

            mb.Entity<ThanhVienNhom>()
                .HasOne(x => x.NhomLamViec).WithMany(x => x.ThanhViens).HasForeignKey(x => x.MaNhom);
            mb.Entity<ThanhVienNhom>()
                .HasOne(x => x.ThanhVien).WithMany(x => x.ThanhVienNhoms).HasForeignKey(x => x.MaThanhVien);

            mb.Entity<CongViec>()
                .HasOne(x => x.NguoiGiao).WithMany().HasForeignKey(x => x.MaNguoiGiao);
            mb.Entity<CongViec>()
                .HasOne(x => x.NguoiThucHien).WithMany().HasForeignKey(x => x.MaNguoiThucHien);
            mb.Entity<CongViec>()
                .HasOne(x => x.NhomLamViec).WithMany(x => x.CongViecs).HasForeignKey(x => x.MaNhom);

            mb.Entity<TraoDoiBinhLuan>()
                .HasOne(x => x.NguoiGui).WithMany().HasForeignKey(x => x.MaNguoiGui);
            mb.Entity<TraoDoiBinhLuan>()
                .HasOne(x => x.CongViec).WithMany(x => x.BinhLuans).HasForeignKey(x => x.MaCongViec);
            mb.Entity<TraoDoiBinhLuan>()
                .HasOne(x => x.NhomLamViec).WithMany().HasForeignKey(x => x.MaNhom);

            mb.Entity<ThongBao>()
                .HasOne(x => x.NguoiNhan).WithMany().HasForeignKey(x => x.MaNguoiNhan);

            // Không xóa dây chuyền (tránh nhiều đường cascade trong SQL Server)
            foreach (var fk in mb.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
                fk.DeleteBehavior = DeleteBehavior.Restrict;

            // ----- Global Query Filter: cô lập dữ liệu theo doanh nghiệp -----
            mb.Entity<ThanhVienDoanhNghiep>().HasQueryFilter(x => x.MaDoanhNghiep == TenantId);
            mb.Entity<NhomLamViec>().HasQueryFilter(x => x.MaDoanhNghiep == TenantId);
            mb.Entity<ThanhVienNhom>().HasQueryFilter(x => x.NhomLamViec.MaDoanhNghiep == TenantId);
            mb.Entity<CongViec>().HasQueryFilter(x => x.NhomLamViec.MaDoanhNghiep == TenantId);
            mb.Entity<TraoDoiBinhLuan>().HasQueryFilter(x => x.NguoiGui.MaDoanhNghiep == TenantId);
        }
    }
}
