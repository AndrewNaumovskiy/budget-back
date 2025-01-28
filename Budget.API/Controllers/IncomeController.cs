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
    public async Task<ActionResult<ResponseModel<GetIncomeData, IError>>> GetIncomes(
        [FromQuery] DateTime from, DateTime to, string? sortBy, int? account, int? category)
    {
        var expenses = await _incomeService.GetIncomes(from, to, sortBy, account, category);

        return Ok(new ResponseModel<GetIncomeData, IError>()
        {
            Data = new GetIncomeData(expenses)
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
