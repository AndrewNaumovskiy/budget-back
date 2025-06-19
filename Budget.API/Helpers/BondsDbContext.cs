using Microsoft.EntityFrameworkCore;
using Budget.API.Models.DbModels.Bonds;

namespace Budget.API.Helpers;

public class BondsDbContext : DbContext
{
    public DbSet<BondDbModel> Bonds { get; set; }
    public DbSet<ActiveDbModel> Actives { get; set; }
    public DbSet<CostHistoryDbModel> CostHistory { get; set; }
    public DbSet<PaymentDbModel> Payments { get; set; }

    public BondsDbContext(DbContextOptions<BondsDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ActiveDbModel>()
                    .HasOne(x => x.Bond)
                    .WithMany(x => x.Actives)
                    .HasForeignKey(x => x.BondId)
                    .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CostHistoryDbModel>()
                    .HasOne(x => x.Bond)
                    .WithMany(x => x.CostHistory)
                    .HasForeignKey(x => x.BondId)
                    .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PaymentDbModel>()
                    .HasOne(x => x.Bond)
                    .WithMany(x => x.Payments)
                    .HasForeignKey(x => x.BondId)
                    .OnDelete(DeleteBehavior.Cascade);
    }
}
