using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using DRSIBOX.Models;

namespace DRSIBOX.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Company> Companies => Set<Company>();

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            base.ConfigureConventions(configurationBuilder);
            // Oracle 21c does not support BOOLEAN as a SQL column type (added in 23c)
            configurationBuilder.Properties<bool>().HaveColumnType("NUMBER(1)");
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Reduce column lengths so Oracle can index them (default 450 exceeds Oracle limits)
            builder.Entity<ApplicationUser>(b =>
            {
                b.Property(u => u.UserName).HasMaxLength(128);
                b.Property(u => u.NormalizedUserName).HasMaxLength(128);
                b.Property(u => u.Email).HasMaxLength(128);
                b.Property(u => u.NormalizedEmail).HasMaxLength(128);
            });

            builder.Entity<IdentityRole>(b =>
            {
                b.Property(r => r.Name).HasMaxLength(128);
                b.Property(r => r.NormalizedName).HasMaxLength(128);
            });

            builder.Entity<ApplicationUser>()
                .HasOne(u => u.Company)
                .WithMany(c => c.Users)
                .HasForeignKey(u => u.CompanyId)
                .IsRequired(false);
        }
    }
}
