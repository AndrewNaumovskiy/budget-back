using Budget.API.Models.DbModels;
using Microsoft.EntityFrameworkCore;

namespace Budget.API.Helpers;

public class BudgetDbContext : DbContext
{
    public DbSet<IncomeDbModel> Incomes { get; set; }
    public DbSet<ExpenseDbModel> Expenses { get; set; }
    
    public DbSet<AccountDbModel> Accounts { get; set; }
    public DbSet<CategoryDbModel> Categories { get; set; }

    public BudgetDbContext(DbContextOptions<BudgetDbContext> options) : base(options) { }

    protected override async void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<IncomeDbModel>()
                    .HasOne(x => x.Account)
                    .WithMany(x => x.Incomes)
                    .HasForeignKey(x => x.AccountId);

        modelBuilder.Entity<IncomeDbModel>()
                    .HasOne(x => x.Category)
                    .WithMany(x => x.Incomes)
                    .HasForeignKey(x => x.CategoryId);

        modelBuilder.Entity<ExpenseDbModel>()
                    .HasOne(x => x.Account)
                    .WithMany(x => x.Expenses)
                    .HasForeignKey(x => x.AccountId);

        modelBuilder.Entity<ExpenseDbModel>()
                    .HasOne(x => x.Category)
                    .WithMany(x => x.Expenses)
                    .HasForeignKey(x => x.CategoryId);
    }
}
