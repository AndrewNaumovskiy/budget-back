using Budget.API.Helpers;
using Budget.API.Models.Dtos;
using Budget.API.Models.DbModels;
using Budget.API.Models.RequestModels;
using Microsoft.EntityFrameworkCore;

namespace Budget.API.Services;

public class IncomeService
{
    private readonly IDbContextFactory<BudgetDbContext> _dbContext;

    public IncomeService(IDbContextFactory<BudgetDbContext> dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddExpense(AddIncomeRequestModel request)
    {
        var expensesRecord = new TransactionDbModel()
        {
            Amount = request.Amount,
            AccountId = request.AccountId,
            CategoryId = request.CategoryId,
            Date = request.Date,
            Desc = request.Description,
            Type = TransactionType.Income,
            BalanceAfterTransaction = 0
        };

        using (var db = await _dbContext.CreateDbContextAsync())
        {
            var currentBalance = (await db.Accounts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.AccountId))!.Balance;

            expensesRecord.BalanceAfterTransaction = Math.Round(currentBalance + request.Amount, 2);

            await db.Transactions.AddAsync(expensesRecord);
            await db.SaveChangesAsync();
        }
    }

    public async Task<List<TransactionDto>> GetIncomes(DateTime from, DateTime to)
    {
        var start = new DateTime(from.Year, from.Month, 1, 0, 0, 0);

        var daysInMonth = DateTime.DaysInMonth(to.Year, to.Month);
        var end = new DateTime(to.Year, to.Month, daysInMonth, 23, 59, 59);

        using (var db = await _dbContext.CreateDbContextAsync())
        {
            return await db.Transactions.AsNoTracking()
                                        .Where(x => x.Type == TransactionType.Income)
                                        .Where(x => x.Date >= start && x.Date <= end)
                                        .OrderByDescending(x => x.Date)
                                        .ThenByDescending(x => x.Id)
                                        .Select(x => new TransactionDto()
                                        {
                                            Id = x.Id,
                                            Type = TransactionType.Income,
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

    public async Task<List<CategoryDto>> GetCategoriesMeow()
    {
        List<CategoryDto> result = new();

        using (var db = await _dbContext.CreateDbContextAsync())
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
