using Budget.API.Models;
using Budget.API.Services;
using Budget.API.Models.RequestModels;
using Microsoft.AspNetCore.Mvc;

namespace Budget.API.Controllers;

[Route("apibudget/expenses")]
[ApiController]
public class ExpensesController : ControllerBase
{
    private readonly ExpenseService _expensesService;

    public ExpensesController(ExpenseService expensesService)
    {
        _expensesService = expensesService;
    }

    [HttpGet]
    public async Task<ActionResult<ResponseModel<GetExpensesData, IError>>> GetExpenses([FromQuery] DateTime from, DateTime to)
    {
        var expenses = await _expensesService.GetExpenses(from, to);

        return Ok(new ResponseModel<GetExpensesData, IError>()
        {
            Data = new GetExpensesData(expenses)
        });
    }

    [HttpPost]
    public async Task<ActionResult<ResponseModel<StatusResponseData, IError>>> AddExpenses([FromBody] AddExpensesRequestModel request)
    {
        await _expensesService.AddExpense(request);

        return Ok(new ResponseModel<StatusResponseData, IError>()
        {
            Data = new StatusResponseData("Expenses added successfully")
        });
    }

    [HttpGet]
    [Route("categories")]
    public async Task<ActionResult<ResponseModel<GetCategoriesData, IError>>> GetCategories([FromQuery] string? page)
    {
        var categories = await _expensesService.GetCategoriesMeow();

        return Ok(new ResponseModel<GetCategoriesData, IError>()
        {
            Data = new GetCategoriesData(categories)
        });
    }

    //[HttpGet]
    //public async Task<ActionResult<ResponseModel<GetCategoriesData, IError>> GetCategories([FromQuery] bool inUah)
    //{
    //    var balance = await _expensesService.GetBalance(inUah);
    //    return Ok(new ResponseModel<AccountBalanceModel, IError>()
    //    {
    //        Data = balance
    //    });
    //}
}
