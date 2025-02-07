using Budget.API.Helpers;
using Budget.API.Models.DbModels;
using Budget.API.Models.RequestModels;
using Microsoft.EntityFrameworkCore;

namespace Budget.API.Services;

public class TransferService
{
    private const int TransferSubCategoryId = 35;

    public TransferService()
    {
        // TODO: find Id of Transfer subcategory
    }

    public async Task AddTransfer(AddTransferRequestModel request, DbContextOptions<BudgetDbContext> dbOptions)
    {
        var fromTransaction = new TransactionDbModel()
        {
            Date = request.Date,
            Amount = request.Amount,
            AccountId = request.FromAccountId,
            CategoryId = TransferSubCategoryId,
            Desc = request.Description,
            Type = TransactionType.TransferFrom
        };

        var toTransaction = new TransactionDbModel()
        {
            Date = request.Date,
            Amount = request.Amount,
            AccountId = request.ToAccountId,
            CategoryId = TransferSubCategoryId,
            Desc = request.Description,
            Type = TransactionType.TransferTo
        };

        using (var db = new BudgetDbContext(dbOptions))
        {
            var fromAccount = await db.Accounts.FirstOrDefaultAsync(x => x.Id == request.FromAccountId);
            var toAccount = await db.Accounts.FirstOrDefaultAsync(x => x.Id == request.ToAccountId);

            var fromNewBalance = Math.Round(fromAccount.Balance - request.Amount, 2);
            var toNewBalance = Math.Round(toAccount.Balance + request.Amount, 2);

            fromAccount.Balance = fromNewBalance;
            toAccount.Balance = toNewBalance;

            fromTransaction.BalanceAfterTransaction = fromNewBalance;
            toTransaction.BalanceAfterTransaction = toNewBalance;

            await db.Transactions.AddRangeAsync(fromTransaction, toTransaction);

            await db.SaveChangesAsync();
        }
    }
}
