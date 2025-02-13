using Budget.API.Helpers;
using Budget.API.Models.Dtos;
using Budget.API.Models.DbModels;
using Budget.API.Models.RequestModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Budget.API.Services;

public class ExpenseService
{
    private readonly IMemoryCache _cache;
    private readonly CurrencyRateService _currencyRateService;

    public ExpenseService(IMemoryCache cache, CurrencyRateService currencyRateService)
    {
        _cache = cache;
        _currencyRateService = currencyRateService;
    }

    public async Task AddExpense(AddExpensesRequestModel request, DbContextOptions<BudgetDbContext> dbOptions)
    {
        var currencyRate = await _currencyRateService.GetUsdToUah();

        var expensesRecord = new TransactionDbModel()
        {
            Amount = request.Amount,
            AccountId = request.AccountId,
            CategoryId = request.CategoryId,
            Date = request.Date,
            Desc = request.Description,
            Type = TransactionType.Expense,
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

    public async Task<List<TransactionDto>> GetExpenses(DateTime from, DateTime to, string? sortBy, int? account, int? category, DbContextOptions<BudgetDbContext> dbOptions)
    {
        var start = new DateTime(from.Year, from.Month, from.Day, 0, 0, 0);
        var end = new DateTime(to.Year, to.Month, to.Day, 23, 59, 59);

        using (var db = new BudgetDbContext(dbOptions))
        {
            var query = db.Transactions.AsNoTracking()
                                       .Where(x => x.Type == TransactionType.Expense)
                                       .Where(x => x.Date >= start && x.Date <= end);

            if (string.IsNullOrEmpty(sortBy) || sortBy == SortBy.Date)
                query = query.OrderByDescending(x => x.Date)
                             .ThenByDescending(x => x.Id);
            else if (sortBy == SortBy.Amount)
                query = query.OrderByDescending(x => x.Amount);

            if(account.HasValue)
                query = query.Where(x => x.AccountId == account);

            if(category.HasValue)
                query = query.Where(x => x.CategoryId == category);

            return await query.Select(x => new TransactionDto()
                                            {
                                                Id = x.Id,
                                                Type = TransactionType.Expense,
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

    public async Task<List<CategoryDto>> GetCategories(string username, DbContextOptions<BudgetDbContext> dbOptions)
    {
        if(_cache.TryGetValue($"EXT_CATEG_{username}", out List<CategoryDto> categories))
        {
            return categories;
        }

        List<CategoryDto> result = new();

        using (var db = new BudgetDbContext(dbOptions))
        {
            var temp = await db.Categories.AsNoTracking()
                                          .Where(x => !(x.Id == 0 || x.Id == 9 || x.Id == 10))
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

        _cache.Set($"EXT_CATEG_{username}", result);

        return result;
    }

    // TELEGRAM ---

    public async Task<List<string>> GetSubCategories(string username, DbContextOptions<BudgetDbContext> dbOptions, string category)
    {
        List<SubCategoryDto> subCategories = null;

        if(!_cache.TryGetValue($"EXT_CATEG_{username}", out List<CategoryDto> categories))
        {
            categories = await GetCategories(username, dbOptions);
        }

        subCategories = categories.FirstOrDefault(x => x.Name == category).SubCategories;
        return subCategories.Select(x => x.Name).ToList();
    }

    public async Task<int> GetCategoryIdByName(string username, DbContextOptions<BudgetDbContext> dbOptions, string subCategoryName)
    {
        List<SubCategoryDto> subCategories = null;

        if (!_cache.TryGetValue($"EXT_CATEG_{username}", out List<CategoryDto> categories))
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

    // TODO: fix this
    public async Task<List<TransactionDto>> GetExpenses(int pageSize, int page = 0)
    {
        using (var db = new BudgetDbContext(null))
        {
            return await db.Transactions.AsNoTracking()
                                        .Where(x => x.Type == TransactionType.Expense)
                                        .OrderByDescending(x => x.Date)
                                        .ThenByDescending(x => x.Id)
                                        .Skip(page * pageSize)
                                        .Take(pageSize)
                                        .Select(x => new TransactionDto()
                                        {
                                            Id = x.Id,
                                            Type = TransactionType.Expense,
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
}
