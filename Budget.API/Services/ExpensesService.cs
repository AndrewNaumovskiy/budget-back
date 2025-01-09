using Budget.API.Helpers;
using Budget.API.Models.DbModels;
using Budget.API.Models.RequestModels;
using Microsoft.EntityFrameworkCore;

namespace Budget.API.Services;

public class ExpensesService
{
    private readonly IDbContextFactory<BudgetDbContext> _dbContext;

    public ExpensesService(IDbContextFactory<BudgetDbContext> dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddExpense(AddExpensesRequestModel request)
    {
        var expensesRecord = new ExpenseDbModel()
        {
            Amount = request.Amount,
            AccountId = request.AccountId,
            CategoryId = request.CategoryId,
            Date = request.Date,
            Desc = request.Description
        };

        using(var db = await _dbContext.CreateDbContextAsync())
        {
            await db.Expenses.AddAsync(expensesRecord);
            await db.SaveChangesAsync();
        }
    }

    public async Task<List<string>> GetCategories()
    {
        return await Task.FromResult<List<string>>(["транспорт", "їжа", "ресторани/кафе", "інші", "сервіси", "донати", "дозвілля", "допомога сім'ї", "шоколадки/чіпсікі", "інтернет/зв'язок", "здоров'я/краса", "житло", "цифрові покупки", "хоз. продукти", "подорожі", "доставка їжі", "подарунки", "одяг", "освіта", "крупні витрати"]);
    }

    public async Task<List<string>> GetAccounts()
    {
        return await Task.FromResult<List<string>>(["UkrSib", "Privat", "Cash"]);
    }
}
