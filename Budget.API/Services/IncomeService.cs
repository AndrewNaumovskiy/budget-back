using Budget.API.Helpers;
using Budget.API.Models.Dtos;
using Budget.API.Models.DbModels;
using Budget.API.Models.RequestModels;
using Microsoft.EntityFrameworkCore;

namespace Budget.API.Services;

public class IncomeService
{
    private readonly CurrencyRateService _currencyRateService;

    public IncomeService(CurrencyRateService currencyRateService)
    {
        _currencyRateService = currencyRateService;
    }

    public async Task AddIncome(AddIncomeRequestModel request, DbContextOptions<BudgetDbContext> dbOptions)
    {
        var currencyRate = await _currencyRateService.GetUsdToUah();

        var expensesRecord = new TransactionDbModel()
        {
            Amount = request.Amount,
            AccountId = request.AccountId,
            CategoryId = request.CategoryId,
            Date = request.Date,
            Desc = request.Description,
            Type = TransactionType.Income,
            BalanceAfterTransaction = 0,
            CurrencyRate = currencyRate
        };

        using (var db = new BudgetDbContext(dbOptions))
        {
            var account = await db.Accounts.FirstOrDefaultAsync(x => x.Id == request.AccountId);

            var newBalance = Math.Round(account.Balance + request.Amount, 2);
            expensesRecord.BalanceAfterTransaction = newBalance;

            account.Balance = newBalance;

            await db.Transactions.AddAsync(expensesRecord);
            await db.SaveChangesAsync();
        }
    }

    public async Task<List<TransactionDto>> GetIncomes(DateTime from, DateTime to, string? sortBy, int? account, int? category, DbContextOptions<BudgetDbContext> dbOptions)
    {
        var start = new DateTime(from.Year, from.Month, from.Day, 0, 0, 0);
        var end = new DateTime(to.Year, to.Month, to.Day, 23, 59, 59);

        using (var db = new BudgetDbContext(dbOptions))
        {
            var query = db.Transactions.AsNoTracking()
                                       .Where(x => x.Type == TransactionType.Income)
                                       .Where(x => x.Date >= start && x.Date <= end);

            if (string.IsNullOrEmpty(sortBy) || sortBy == SortBy.Date)
                query = query.OrderByDescending(x => x.Date)
                             .ThenByDescending(x => x.Id);
            else if (sortBy == SortBy.Amount)
                query = query.OrderByDescending(x => x.Amount);

            if (account.HasValue)
                query = query.Where(x => x.AccountId == account);

            if (category.HasValue)
                query = query.Where(x => x.CategoryId == category);

            return await query.Select(x => new TransactionDto()
                                            {
                                                Id = x.Id,
                                                Type = TransactionType.Income,
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

    public async Task<List<CategoryDto>> GetCategoriesMeow(DbContextOptions<BudgetDbContext> dbOptions)
    {
        List<CategoryDto> result = new();

        using (var db = new BudgetDbContext(dbOptions))
        {
            var temp = await db.Categories.AsNoTracking()
                                          .Where(x => x.Id == 0)
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

        return result;
    }
}
