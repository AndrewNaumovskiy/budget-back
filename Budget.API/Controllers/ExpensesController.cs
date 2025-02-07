using Budget.API.Models;
using Budget.API.Services;
using Budget.API.Models.RequestModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Budget.API.Controllers;

[Route("apibudget/expenses")]
[ApiController]
public class ExpensesController : ControllerBase
{
    private readonly ExpenseService _expensesService;
    private readonly DatabaseSelectorService _databaseSelectorService;

    public ExpensesController(ExpenseService expensesService, DatabaseSelectorService databaseSelectorService)
    {
        _expensesService = expensesService;
        _databaseSelectorService = databaseSelectorService;
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<ResponseModel<GetExpensesData, IError>>> GetExpenses(
        [FromQuery] DateTime from, DateTime to, string? sortBy, int? account, int? category)
    {
        var dbOptions = _databaseSelectorService.GetUserDatabase(User.Identity.Name);

        var expenses = await _expensesService.GetExpenses(from, to, sortBy, account, category, dbOptions);

        return Ok(new ResponseModel<GetExpensesData, IError>()
        {
            Data = new GetExpensesData(expenses)
        });
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<ResponseModel<StatusResponseData, IError>>> AddExpenses([FromBody] AddExpensesRequestModel request)
    {
        var dbOptions = _databaseSelectorService.GetUserDatabase(User.Identity.Name);

        await _expensesService.AddExpense(request, dbOptions);

        return Ok(new ResponseModel<StatusResponseData, IError>()
        {
            Data = new StatusResponseData("Expenses added successfully")
        });
    }

    [HttpGet]
    [Authorize]
    [Route("categories")]
    public async Task<ActionResult<ResponseModel<GetCategoriesData, IError>>> GetCategories()
    {
        var dbOptions = _databaseSelectorService.GetUserDatabase(User.Identity.Name);

        var categories = await _expensesService.GetCategoriesMeow(dbOptions);

        return Ok(new ResponseModel<GetCategoriesData, IError>()
        {
            Data = new GetCategoriesData(categories)
        });
    }
}
