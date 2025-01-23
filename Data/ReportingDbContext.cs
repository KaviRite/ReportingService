using Microsoft.EntityFrameworkCore;
using ReportingService.Models;
using System.Collections.Generic;

namespace ReportingService.Data
{
    public class ReportingDbContext : DbContext
    {
        public ReportingDbContext(DbContextOptions<ReportingDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Address> Addresses { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; }

        //public virtual DbSet<UserInfo>? UserInfos { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Order>()
                .HasOne(o => o.User)
                .WithMany(u => u.Orders)
                .HasForeignKey(o => o.UserId);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.Product)
                .WithMany(p => p.Orders)
                .HasForeignKey(o => o.ProductId);

            //modelBuilder.Entity<UserInfo>(entity =>
            //{
            //    entity.HasNoKey();
            //    entity.ToTable("UserInfo");
            //    entity.Property(e => e.UserId).HasColumnName("UserId");
            //    entity.Property(e => e.DisplayName).HasMaxLength(60).IsUnicode(false);
            //    entity.Property(e => e.UserName).HasMaxLength(30).IsUnicode(false);
            //    entity.Property(e => e.Email).HasMaxLength(50).IsUnicode(false);
            //    entity.Property(e => e.Password).HasMaxLength(20).IsUnicode(false);
            //    entity.Property(e => e.CreatedDate).IsUnicode(false);
            //});
        }
    }
}
