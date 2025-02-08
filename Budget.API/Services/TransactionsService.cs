using Budget.API.Helpers;
using Budget.API.Models.Dtos;
using Budget.API.Models.RequestModels;
using Microsoft.EntityFrameworkCore;

namespace Budget.API.Services;

public class TransactionsService
{
    public async Task<TransactionDto?> GetTransaction(int id, DbContextOptions<BudgetDbContext> dbOptions)
    {
        using (var db = new BudgetDbContext(dbOptions))
        {
            return await db.Transactions.AsNoTracking()
                                        .Where(x => x.Id == id)
                                        .Select(x => new TransactionDto()
                                        {
                                            Id = x.Id,
                                            Type = x.Type,
                                            Date = x.Date,
                                            Amount = x.Amount,
                                            Description = x.Desc,
                                            AccountId = x.Account.Id,
                                            AccountName = x.Account.Name,
                                            CategoryId = x.Category.Id,
                                            CategoryName = x.Category.Name,
                                            Balance = x.BalanceAfterTransaction
                                        })
                                        .FirstOrDefaultAsync();
        }
    }

    public async Task<bool> EditTransaction(int id, EditTransactionRequestModel transaction, DbContextOptions<BudgetDbContext> dbOptions)
    {
        using (var db = new BudgetDbContext(dbOptions))
        {
            var dbTransaction = await db.Transactions.FirstOrDefaultAsync(x => x.Id == id);
            if (dbTransaction == null)
                return false;

            dbTransaction.Type = transaction.Type;
            dbTransaction.Date = transaction.Date;
            dbTransaction.Amount = transaction.Amount;
            dbTransaction.Desc = transaction.Description;
            dbTransaction.AccountId = transaction.AccountId;
            dbTransaction.CategoryId = transaction.CategoryId;

            // TODO: update balance of rest of the transactions

            await db.SaveChangesAsync();
        }
        return true;
    }

    public async Task<bool> DeleteTransaction(int id, DbContextOptions<BudgetDbContext> dbOptions)
    {
        using (var db = new BudgetDbContext(dbOptions))
        {
            var dbTransaction = await db.Transactions.FirstOrDefaultAsync(x => x.Id == id);
            if (dbTransaction == null)
                return false;

            db.Transactions.Remove(dbTransaction);
            // TODO: update balance of rest of the transactions

            await db.SaveChangesAsync();
        }
        return true;
    }

    public async Task<List<TransactionDto>> GetRecentTransactions(string pageStr, DbContextOptions<BudgetDbContext> dbOptions)
    {
        const int PageSize = 5;
        int page = 0;
        if (!string.IsNullOrEmpty(pageStr) && int.TryParse(pageStr, out int pageParsed))
        {
            page = pageParsed;
        }

        using (var db = new BudgetDbContext(dbOptions))
        {
            return await db.Transactions.AsNoTracking()
                                        .OrderByDescending(x => x.Date)
                                        .ThenByDescending(x => x.Id)
                                        //.Skip(page * PageSize)
                                        .Take(PageSize)
                                        .Select(x => new TransactionDto()
                                        {
                                            Id = x.Id,
                                            Type = x.Type,
                                            Date = x.Date,
                                            Amount = x.Amount,
                                            Description = x.Desc,
                                            AccountId = x.Account.Id,
                                            AccountName = x.Account.Name,
                                            CategoryId = x.Category.Id,
                                            CategoryName = x.Category.Name,
                                            Balance = x.BalanceAfterTransaction
                                        })
                                        .ToListAsync();
        }
    }

    public async Task<(double, double, double, double)> GetSummary(string yearStr, string monthStr, DbContextOptions<BudgetDbContext> dbOptions)
    {
        double income = 0, expenses = 0, savings = 0, unspecified = 0;

        var now = DateTime.UtcNow;

        int year = now.Year;
        int month = now.Month;

        if (!string.IsNullOrEmpty(yearStr) && int.TryParse(yearStr, out int yearParsed))
        {
            year = yearParsed;
        }
        if (!string.IsNullOrEmpty(monthStr) && int.TryParse(monthStr, out int monthParsed))
        {
            month = monthParsed;
        }

        DateTime from = new DateTime(year, month, 1, 0, 0, 1);
        DateTime to = from.AddMonths(1).AddSeconds(-1);

        using (var db = new BudgetDbContext(dbOptions))
        {
            var transactions = await db.Transactions.AsNoTracking()
                                                    .Where(x => x.Date >= from && x.Date <= to)
                                                    .ToListAsync();

            income = transactions.Where(x => x.Type == TransactionType.Income).Sum(x => x.Amount);
            expenses = transactions.Where(x => x.Type == TransactionType.Expense).Sum(x => x.Amount);
            savings = transactions.Where(x => x.Type == TransactionType.Savings).Sum(x => x.Amount);
            unspecified = income - expenses - savings;
        }

        return (Math.Round(income, 2), Math.Round(expenses, 2), Math.Round(savings, 2), Math.Round(unspecified, 2));
    }
}
