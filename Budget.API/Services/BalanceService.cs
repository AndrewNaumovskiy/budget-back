using Budget.API.Helpers;
using Budget.API.Models;
using Budget.API.Models.Dtos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Budget.API.Services;

public class BalanceService
{
    private readonly IMemoryCache _cache;
    private readonly CurrencyRateService _currencyRateService;

    public BalanceService(IMemoryCache cache, CurrencyRateService currencyRateService)
    {
        _cache = cache;
        _currencyRateService = currencyRateService;
    }

    public async Task<List<AccountDto>> GetAccounts(string username, DbContextOptions<BudgetDbContext> dbOptions)
    {
        if (_cache.TryGetValue($"ACCS_{username}", out List<AccountDto> accounts))
        {
            return accounts;
        }

        using (var db = new BudgetDbContext(dbOptions))
        {
            accounts = await db.Accounts.AsNoTracking()
                                    .Select(x => new AccountDto()
                                    {
                                        Id = x.Id,
                                        Name = x.Name,
                                        Balance = x.Balance
                                    })
                                    .ToListAsync();

            _cache.Set($"ACCS_{username}", accounts);

            return accounts;
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


    // TELEGRAM ---

    public async Task<List<string>> GetBalance(DbContextOptions<BudgetDbContext> dbOptions, bool inUah)
    {
        double currencyRate = 0.0;
        if (!inUah)
        {
            var temp = await _currencyRateService.GetUsdToUah();
            currencyRate = 1 / temp;
        }

        using (var db = new BudgetDbContext(dbOptions))
        {
            var accounts = await db.Accounts.AsNoTracking()
                                            .ToListAsync();

            var balances = accounts.Select(x => new AccountBalanceModel(x, currencyRate, inUah)).ToList();
            var total = balances.Sum(x => x.Amount);
            balances.Add(new AccountBalanceModel(total, inUah));

            return balances.Select(x => x.ToString()).ToList();
        }
    }

    public async Task<int> GetAccountIdByName(string username, DbContextOptions<BudgetDbContext> dbOptions, string accountName)
    {
        if (!_cache.TryGetValue($"ACCS_{username}", out List<AccountDto> accounts))
        {
            accounts = await GetAccounts(username, dbOptions);
        }

        return accounts.FirstOrDefault(x => x.Name == accountName).Id;
    }

    public double GetCurrentBalance(int accountId, DbContextOptions<BudgetDbContext> dbOptions)
    {
        using(var db = new BudgetDbContext(dbOptions))
        {
            return db.Accounts.AsNoTracking()
                              .FirstOrDefault(x => x.Id == accountId)
                              .Balance;
        }
    }
}
