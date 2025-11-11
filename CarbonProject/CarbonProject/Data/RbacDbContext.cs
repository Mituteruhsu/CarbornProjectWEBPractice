using CarbonProject.Models.EFModels.RBAC;
using Microsoft.EntityFrameworkCore;

namespace CarbonProject.Data
{
    public class RbacDbContext : DbContext
    {
        public RbacDbContext(DbContextOptions<RbacDbContext> options) : base(options) { }

        // RBAC Tables
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Role> Roles { get; set; } = null!;
        public DbSet<Permission> Permissions { get; set; } = null!;
        public DbSet<RolePermission> RolePermissions { get; set; } = null!;
        public DbSet<Capability> Capabilities { get; set; } = null!;
        public DbSet<PermissionCapability> PermissionCapabilities { get; set; } = null!;
        public DbSet<UserRole> UserRoles { get; set; } = null!;
        public DbSet<UserCompanyRole> UserCompanyRoles { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 多對多 PKs
            modelBuilder.Entity<RolePermission>()
                .HasKey(rp => new { rp.RoleId, rp.PermissionId });

            modelBuilder.Entity<PermissionCapability>()
                .HasKey(pc => new { pc.PermissionId, pc.CapabilityId });

            modelBuilder.Entity<UserRole>()
                .HasKey(ur => new { ur.MemberId, ur.RoleId });

            // UserCompanyRole FK
            modelBuilder.Entity<UserCompanyRole>()
                .HasOne(ucr => ucr.User)
                .WithMany(u => u.UserCompanyRoles)
                .HasForeignKey(ucr => ucr.MemberId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserCompanyRole>()
                .HasOne(ucr => ucr.Role)
                .WithMany(r => r.UserCompanyRoles)
                .HasForeignKey(ucr => ucr.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            // RolePermission FK
            modelBuilder.Entity<RolePermission>()
                .HasOne(rp => rp.Role)
                .WithMany(r => r.RolePermissions)
                .HasForeignKey(rp => rp.RoleId);

            modelBuilder.Entity<RolePermission>()
                .HasOne(rp => rp.Permission)
                .WithMany(p => p.RolePermissions)
                .HasForeignKey(rp => rp.PermissionId);

            // PermissionCapability FK
            modelBuilder.Entity<PermissionCapability>()
                .HasOne(pc => pc.Permission)
                .WithMany(p => p.PermissionCapabilities)
                .HasForeignKey(pc => pc.PermissionId);

            modelBuilder.Entity<PermissionCapability>()
                .HasOne(pc => pc.Capability)
                .WithMany(c => c.PermissionCapabilities)
                .HasForeignKey(pc => pc.CapabilityId);

            // UserRole FK
            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.MemberId);

            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId);
        }
    }
}
