using Budget.API.Helpers;
using Budget.API.Models;
using Budget.API.Models.Dtos;
using Microsoft.EntityFrameworkCore;

namespace Budget.API.Services;

public class BalanceService
{
    private readonly IDbContextFactory<BudgetDbContext> _dbContext;

    public BalanceService(IDbContextFactory<BudgetDbContext> dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AccountBalanceModel> GetBalance(bool inUah)
    {
        double currencyRate = 0.0;
        if (!inUah)
        {
            // TODO: get currency rate from API
            currencyRate = 0.0237048275;
        }

        using (var db = await _dbContext.CreateDbContextAsync())
        {
            var accounts = await db.Accounts.ToListAsync();
            double ukrsib = accounts[0].Balance;
            double privat = accounts[1].Balance;
            double cash = accounts[2].Balance;

            return new AccountBalanceModel(ukrsib, privat, cash, currencyRate, inUah);
        }
    }


    public async Task<(double, double, double, double)> GetSummary(string yearStr, string monthStr)
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

        using (var db = await _dbContext.CreateDbContextAsync())
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

    public async Task<List<ExpenseDto>> GetRecentTransactions(string pageStr)
    {
        const int PageSize = 5;
        int page = 0;
        if (!string.IsNullOrEmpty(pageStr) && int.TryParse(pageStr, out int pageParsed))
        {
            page = pageParsed;
        }

        using (var db = await _dbContext.CreateDbContextAsync())
        {
            return await db.Transactions.AsNoTracking()
                                        .OrderByDescending(x => x.Date)
                                        .ThenByDescending(x => x.Id)
                                        //.Skip(page * PageSize)
                                        .Take(PageSize)
                                        .Select(x => new ExpenseDto()
                                        {
                                            Id = x.Id,
                                            Date = x.Date,
                                            Amount = x.Amount,
                                            Description = x.Desc,
                                            AccountName = x.Account.Name,
                                            CategoryName = x.Category.Name,
                                            Balance = x.BalanceAfterTransaction
                                        })
                                        .ToListAsync();
        }
    }

    public async Task<(List<double>, List<double>)> GetIncomeExpenseChart()
    {
        List<double> incomeRes = new(), expenseRes = new();

        const int monthsCount = 6;

        DateTime now = DateTime.UtcNow;
        DateTime from = new DateTime(now.Year, now.Month, 1, 0,0,1).AddMonths(-monthsCount);

        using (var db = await _dbContext.CreateDbContextAsync())
        {
            var transations = await db.Transactions.AsNoTracking()
                                                   .Where(x => x.Date >= from)
                                                   .OrderBy(x => x.Date)
                                                   .GroupBy(x => new
                                                   {
                                                       x.Date.Year,
                                                       x.Date.Month
                                                   })
                                                   .ToListAsync();

            foreach(var item in transations)
            {
                incomeRes.Add(Math.Round(item.Where(x => x.Type == TransactionType.Income).Sum(x => x.Amount), 2));
                expenseRes.Add(Math.Round(item.Where(x => x.Type == TransactionType.Expense).Sum(x => x.Amount), 2));
            }

            return (incomeRes, expenseRes);
        }
    }

    public double GetCurrentBalance(int accountId)
    {
        return 400;
    }
}
