using CommunicationLTD.Models;
using Microsoft.EntityFrameworkCore;

namespace CommunicationLTD.Data;

public class CommunicationLtdDbContext : DbContext
{
    public CommunicationLtdDbContext(DbContextOptions<CommunicationLtdDbContext> options)
        : base(options)
    {
    }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<InternetPackage> InternetPackages => Set<InternetPackage>();
    public DbSet<Sector> Sectors => Set<Sector>();
    public DbSet<CommunicationLTD.Models.User> Users => Set<CommunicationLTD.Models.User>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<PasswordHistory> PasswordHistories => Set<PasswordHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>()
            .HasOne(c => c.Sector)
            .WithMany(s => s.Customers)
            .HasForeignKey(c => c.SectorId);

        modelBuilder.Entity<Customer>()
            .HasOne(c => c.InternetPackage)
            .WithMany(p => p.Customers)
            .HasForeignKey(c => c.PackageId);

        modelBuilder.Entity<CommunicationLTD.Models.User>(b =>
        {
            b.HasIndex(u => u.Email).IsUnique();
        });

        modelBuilder.Entity<PasswordResetToken>(b =>
        {
            b.HasIndex(t => t.ResetToken).IsUnique();
            b.HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PasswordHistory>(b =>
        {
            b.HasOne(h => h.User)
                .WithMany()
                .HasForeignKey(h => h.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Sector>().HasData(
            new Sector { Id = 1, Name = "משקי בית", Description = "לקוחות פרטיים" },
            new Sector { Id = 2, Name = "עסקים", Description = "לקוחות ארגוניים" },
            new Sector { Id = 3, Name = "ממשלה", Description = "לקוחות ציבוריים" });

        modelBuilder.Entity<InternetPackage>().HasData(
            new InternetPackage { Id = 1, Name = "Basic 100", SpeedMbps = 100, Price = 89.90m, Description = "חבילת גלישה בסיסית" },
            new InternetPackage { Id = 2, Name = "Pro 300", SpeedMbps = 300, Price = 129.90m, Description = "חבילת גלישה מקצועית" },
            new InternetPackage { Id = 3, Name = "Ultra 1000", SpeedMbps = 1000, Price = 199.90m, Description = "חבילת גלישה מהירה במיוחד" });

        modelBuilder.Entity<Customer>().HasData(
            new Customer { Id = 1, FullName = "איילה כהן", Email = "ayala@sample.com", Phone = "050-1111111", SectorId = 1, PackageId = 1 },
            new Customer { Id = 2, FullName = "רוני שמעון", Email = "roni@sample.com", Phone = "050-2222222", SectorId = 2, PackageId = 2 },
            new Customer { Id = 3, FullName = "נעמי ברק", Email = "naomi@sample.com", Phone = "050-3333333", SectorId = 3, PackageId = 3 });
    }
}
