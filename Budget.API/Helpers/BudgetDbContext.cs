using Budget.API.Models.DbModels;
using Microsoft.EntityFrameworkCore;

namespace Budget.API.Helpers;

public class BudgetDbContext : DbContext
{
    public DbSet<TransferDbModel> Transfers { get; set; }
    public DbSet<TransactionDbModel> Transactions { get; set; }
    
    public DbSet<AccountDbModel> Accounts { get; set; }
    public DbSet<CategoryDbModel> Categories { get; set; }
    public DbSet<SubCategoryDbModel> SubCategories { get; set; }

    public BudgetDbContext(DbContextOptions<BudgetDbContext> options) : base(options) { }

    protected override async void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SubCategoryDbModel>()
                    .HasOne(x => x.UpperCategory)
                    .WithMany(x => x.SubCategories)
                    .HasForeignKey(x => x.CategoryId);


        modelBuilder.Entity<TransactionDbModel>()
                    .HasOne(x => x.Account)
                    .WithMany(x => x.Transactions)
                    .HasForeignKey(x => x.AccountId);

        modelBuilder.Entity<TransactionDbModel>()
                    .HasOne(x => x.Category)
                    .WithMany(x => x.Transactions)
                    .HasForeignKey(x => x.CategoryId);
    }
}
