using Budget.API.Helpers;
using Budget.API.Models;
using Budget.API.Models.Dtos;
using Microsoft.EntityFrameworkCore;

namespace Budget.API.Services;

public class BalanceService
{
    private readonly CurrencyRateService _currencyRateService;

    public BalanceService(CurrencyRateService currencyRateService)
    {
        _currencyRateService = currencyRateService;
    }

    public async Task<AccountBalanceModel> GetBalance(bool inUah)
    {
        double currencyRate = 0.0;
        if (!inUah)
        {
            var temp = await _currencyRateService.GetUsdToUah();
            currencyRate = 1 / temp;
        }

        // TODO: fix this
        using (var db = new BudgetDbContext(null))
        {
            var accounts = await db.Accounts.ToListAsync();
            double ukrsib = accounts[0].Balance;
            double privat = accounts[1].Balance;
            double cash = accounts[2].Balance;

            return new AccountBalanceModel(ukrsib, privat, cash, currencyRate, inUah);
        }
    }


    public async Task<List<AccountDto>> GetAccounts(DbContextOptions<BudgetDbContext> dbOptions)
    {
        using(var db = new BudgetDbContext(dbOptions))
        {
            return await db.Accounts.AsNoTracking()
                                    .Select(x => new AccountDto()
                                    {
                                        Id = x.Id,
                                        Name = x.Name,
                                        Balance = x.Balance
                                    })
                                    .ToListAsync();
        }
    }

    public async Task<(List<double>, List<double>)> GetIncomeExpenseChart(DbContextOptions<BudgetDbContext> dbOptions)
    {
        List<double> incomeRes = new(), expenseRes = new();

        const int monthsCount = 5;

        DateTime now = DateTime.UtcNow;
        DateTime from = new DateTime(now.Year, now.Month, 1, 0,0,1).AddMonths(-monthsCount);

        using (var db = new BudgetDbContext(dbOptions))
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

    // TODO: fix this
    public double GetCurrentBalance(int accountId)
    {
        return 400;
    }
}
