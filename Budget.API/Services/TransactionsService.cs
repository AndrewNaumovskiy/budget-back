using Budget.API.Helpers;
using Budget.API.Models.Dtos;
using Microsoft.EntityFrameworkCore;

namespace Budget.API.Services;

public class TransactionsService
{
    private readonly IDbContextFactory<BudgetDbContext> _dbContext;

    public TransactionsService(IDbContextFactory<BudgetDbContext> dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TransactionDto?> GetTransaction(int id)
    {
        using (var db = await _dbContext.CreateDbContextAsync())
        {
            return await db.Transactions.AsNoTracking()
                                        .Where(x => x.Id == id)
                                        .Select(x => new TransactionDto()
                                        {
                                            Id = x.Id,
                                            Type = x.Type,
                                            Date = x.Date,
                                            Amount = x.Amount,
                                            Description = x.Desc,
                                            AccountName = x.Account.Name,
                                            CategoryName = x.Category.Name,
                                            Balance = x.BalanceAfterTransaction
                                        })
                                        .FirstOrDefaultAsync();
        }
    }
}
