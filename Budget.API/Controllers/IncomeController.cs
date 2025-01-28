using Budget.API.Models;
using Budget.API.Services;
using Budget.API.Models.RequestModels;
using Microsoft.AspNetCore.Mvc;

namespace Budget.API.Controllers;

[Route("apibudget/income")]
[ApiController]
public class IncomeController : ControllerBase
{
    private readonly IncomeService _incomeService;

    public IncomeController(IncomeService incomeService)
    {
        _incomeService = incomeService;
    }

    [HttpGet]
    public async Task<ActionResult<ResponseModel<GetRecentTransactionsData, IError>>> GetExpenses([FromQuery] DateTime from, DateTime to)
    {
        var expenses = await _incomeService.GetIncomes(from, to);

        return Ok(new ResponseModel<GetRecentTransactionsData, IError>()
        {
            Data = new GetRecentTransactionsData(expenses)
        });
    }

    [HttpPost]
    public async Task<ActionResult<ResponseModel<StatusResponseData, IError>>> AddIncome([FromBody] AddIncomeRequestModel request)
    {
        await _incomeService.AddExpense(request);

        return Ok(new ResponseModel<StatusResponseData, IError>()
        {
            Data = new StatusResponseData("Income added successfully")
        });
    }

    [HttpGet]
    [Route("categories")]
    public async Task<ActionResult<ResponseModel<GetCategoriesData, IError>>> GetCategories()
    {
        var categories = await _incomeService.GetCategoriesMeow();

        return Ok(new ResponseModel<GetCategoriesData, IError>()
        {
            Data = new GetCategoriesData(categories)
        });
    }
}
