using Budget.API.Helpers;
using Budget.API.Models;
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
