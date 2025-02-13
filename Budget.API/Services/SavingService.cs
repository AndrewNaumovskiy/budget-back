using Budget.API.Helpers;
using Budget.API.Models.Dtos;
using Budget.API.Models.DbModels;
using Budget.API.Models.RequestModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Budget.API.Services;

public class SavingService
{
    private readonly IMemoryCache _cache;
    private readonly CurrencyRateService _currencyRateService;

    public SavingService(IMemoryCache cache, CurrencyRateService currencyRateService)
    {
        _cache = cache;
        _currencyRateService = currencyRateService;
    }

    public async Task AddSaving(AddIncomeRequestModel request, DbContextOptions<BudgetDbContext> dbOptions)
    {
        var currencyRate = await _currencyRateService.GetUsdToUah();

        var expensesRecord = new TransactionDbModel()
        {
            Amount = request.Amount,
            AccountId = request.AccountId,
            CategoryId = request.CategoryId,
            Date = request.Date,
            Desc = request.Description,
            Type = TransactionType.Savings,
            BalanceAfterTransaction = 0,
            CurrencyRate = currencyRate
        };

        using (var db = new BudgetDbContext(dbOptions))
        {
            var account = await db.Accounts.FirstOrDefaultAsync(x => x.Id == request.AccountId);

            var newBalance = Math.Round(account.Balance - request.Amount, 2);
            expensesRecord.BalanceAfterTransaction = newBalance;

            account.Balance = newBalance;

            await db.Transactions.AddAsync(expensesRecord);
            await db.SaveChangesAsync();
        }
    }

    public async Task<List<CategoryDto>> GetCategories(string username, DbContextOptions<BudgetDbContext> dbOptions)
    {
        if (_cache.TryGetValue($"SAV_CATEG_{username}", out List<CategoryDto> categories))
        {
            return categories;
        }

        List<CategoryDto> result = new();

        using (var db = new BudgetDbContext(dbOptions))
        {
            var temp = await db.Categories.AsNoTracking()
                                          .Where(x => x.Id == 9)
                                          .Include(x => x.SubCategories)
                                          .ToListAsync();

            foreach (var item in temp)
            {
                CategoryDto cat = new(item.Id, item.Name);

                foreach (var sub in item.SubCategories)
                {
                    cat.SubCategories.Add(new(sub.Id, sub.Name));
                }
                result.Add(cat);
            }
        }

        _cache.Set($"SAV_CATEG_{username}", result);

        return result;
    }

    public async Task<List<string>> GetSubCategories(string username, DbContextOptions<BudgetDbContext> dbOptions)
    {
        List<SubCategoryDto> subCategories = null;

        if (!_cache.TryGetValue($"SAV_CATEG_{username}", out List<CategoryDto> categories))
        {
            categories = await GetCategories(username, dbOptions);
        }

        subCategories = categories.FirstOrDefault().SubCategories;
        return subCategories.Select(x => x.Name).ToList();
    }

    public async Task<int> GetCategoryIdByName(string username, DbContextOptions<BudgetDbContext> dbOptions, string? subCategoryName)
    {
        List<SubCategoryDto> subCategories = null;

        if (!_cache.TryGetValue($"SAV_CATEG_{username}", out List<CategoryDto> categories))
        {
            categories = await GetCategories(username, dbOptions);
        }

        foreach (var item in categories)
            foreach (var subCat in item.SubCategories)
                if (subCat.Name == subCategoryName)
                {
                    return subCat.Id;
                }

        return -1;
    }
}
