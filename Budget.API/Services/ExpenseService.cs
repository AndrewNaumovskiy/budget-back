﻿using Budget.API.Helpers;
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


    private readonly IDbContextFactory<BudgetDbContext> _dbContext;

    public ExpenseService(IDbContextFactory<BudgetDbContext> dbContext)
    {
        _dbContext = dbContext;

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
        var expensesRecord = new TransactionDbModel()
        {
            Amount = request.Amount,
            AccountId = request.AccountId,
            CategoryId = request.CategoryId,
            Date = request.Date,
            Desc = request.Description,
            Type = TransactionType.Expense,
            BalanceAfterTransaction = 0
        };

        using (var db = await _dbContext.CreateDbContextAsync())
        {
            var currentBalance = (await db.Accounts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.AccountId))!.Balance;

            expensesRecord.BalanceAfterTransaction = Math.Round(currentBalance - request.Amount, 2);

            await db.Transactions.AddAsync(expensesRecord);
            await db.SaveChangesAsync();
        }
    }


    public async Task<List<TransactionDto>> GetExpenses(DateTime from, DateTime to)
    {
        var start = new DateTime(from.Year, from.Month, 1, 0, 0, 0);

        var daysInMonth = DateTime.DaysInMonth(to.Year, to.Month);
        var end = new DateTime(to.Year, to.Month, daysInMonth, 23, 59, 59);

        using (var db = await _dbContext.CreateDbContextAsync())
        {
            return await db.Transactions.AsNoTracking()
                                        .Where(x => x.Type == TransactionType.Expense)
                                        .Where(x => x.Date >= start && x.Date <= end)
                                        .OrderByDescending(x => x.Date)
                                        .ThenByDescending(x => x.Id)
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
                                          .Where(x => !(x.Id == 0 || x.Id == 9))
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
