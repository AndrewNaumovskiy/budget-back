using Budget.API.Helpers;
using Budget.API.Models.DbModels;
using Budget.API.Models.Dtos;
using Budget.API.Models.RequestModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace Budget.API.Services;

public class ExpenseService
{
    private List<CategoryDbModel> _categories = new();
    private List<SubCategoryDbModel> _subCategories = new();
    private List<AccountDbModel> _accounts = new();

    private List<string> _categoriesStr = new();
    private List<string> _subCategoriesStr = new();
    private List<string> _accountsStr = new();

    private readonly CurrencyRateService _currencyRateService;
    private readonly IDbContextFactory<BudgetDbContext> _dbContext;

    public ExpenseService(CurrencyRateService currencyRateService, IDbContextFactory<BudgetDbContext> dbContext)
    {
        _dbContext = dbContext;
        _currencyRateService = currencyRateService;

        _ = InitService();
    }

    public async Task InitService()
    {
        using(var db = await _dbContext.CreateDbContextAsync())
        {
            var categories = await db.Categories.AsNoTracking()
                                                .Include(x => x.SubCategories)
                                                .Skip(1)
                                                .ToListAsync();

            foreach(var item in categories)
            {
                foreach(var subCat in item.SubCategories)
                {
                    _subCategories.Add(subCat);
                }

                item.SubCategories = null;

                _categories.Add(item);
            }

            _accounts = await db.Accounts.AsNoTracking()
                                         .ToListAsync();
        }

        _categoriesStr = _categories.Select(x => x.Name).ToList();
        _subCategoriesStr = _subCategories.Select(x => x.Name).ToList();
        _accountsStr = _accounts.Select(x => x.Name).ToList();
    }

    public async Task AddExpense(AddExpensesRequestModel request)
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

        using (var db = await _dbContext.CreateDbContextAsync())
        {
            var account = await db.Accounts.FirstOrDefaultAsync(x => x.Id == request.AccountId);

            var newBalance = Math.Round(account.Balance - request.Amount, 2);
            expensesRecord.BalanceAfterTransaction = newBalance;

            account.Balance = newBalance;

            await db.Transactions.AddAsync(expensesRecord);
            await db.SaveChangesAsync();
        }
    }


    public async Task<List<TransactionDto>> GetExpenses(DateTime from, DateTime to, string? sortBy, int? account, int? category)
    {
        var start = new DateTime(from.Year, from.Month, from.Day, 0, 0, 0);
        var end = new DateTime(to.Year, to.Month, to.Day, 23, 59, 59);

        using (var db = await _dbContext.CreateDbContextAsync())
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
                                                AccountName = x.Account.Name,
                                                CategoryName = x.Category.Name,
                                                Balance = x.BalanceAfterTransaction
                                            })
                              .ToListAsync();
        }
    }

    public async Task<List<TransactionDto>> GetExpenses(int pageSize, int page = 0)
    {
        using (var db = await _dbContext.CreateDbContextAsync())
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
                                            AccountName = x.Account.Name,
                                            CategoryName = x.Category.Name,
                                            Balance = x.BalanceAfterTransaction
                                        })
                                        .ToListAsync();
        }
    }

    public List<string> GetCategories()
    {
        return _categoriesStr;
    }

    public List<string> GetSubCategories(string categoryName)
    {
        var catId = _categories.First(x => x.Name == categoryName).Id;
        return _subCategories.Where(x => x.CategoryId == catId).Select(x => x.Name).ToList();
    }

    public int GetCategoryIdByName(string name)
    {
        return _subCategories.First(x => x.Name == name).Id;
    }

    public List<string> GetAccounts()
    {
        return _accountsStr;
    }

    public int GetAccountIdByName(string name)
    {
        return _accounts.FirstOrDefault(x => x.Name == name)!.Id;
    }

    public async Task<List<CategoryDto>> GetCategoriesMeow()
    {
        List<CategoryDto> result = new();

        using (var db = await _dbContext.CreateDbContextAsync())
        {
            var temp = await db.Categories.AsNoTracking()
                                          .Where(x => !(x.Id == 0 || x.Id == 9 || x.Id == 10))
                                          .Include(x => x.SubCategories)
                                          .ToListAsync();

            foreach(var item in temp)
            {
                CategoryDto cat = new(item.Id, item.Name);

                foreach(var sub in item.SubCategories)
                {
                    cat.SubCategories.Add(new(sub.Id, sub.Name));
                }
                result.Add(cat);
            }
        }

        return result;
    }
}
