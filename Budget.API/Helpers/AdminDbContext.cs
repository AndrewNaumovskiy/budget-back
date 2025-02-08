using Budget.API.Models.DbModels.Admin;
using Microsoft.EntityFrameworkCore;

namespace Budget.API.Helpers;

public class AdminDbContext : DbContext
{
    public DbSet<UserDbModel> Users { get; set; }
    public DbSet<RefreshTokenDbModel> RefreshTokens { get; set; }

    public AdminDbContext(DbContextOptions<AdminDbContext> options) : base(options) { }

    protected override async void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RefreshTokenDbModel>()
                    .HasOne(x => x.User)
                    .WithMany(x => x.RefreshTokens)
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
    }
}
