using Budget.API.Models;
using Budget.API.Services;
using Budget.API.Models.RequestModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Budget.API.Controllers;

[Route("apibudget/income")]
[ApiController]
public class IncomeController : ControllerBase
{
    private readonly IncomeService _incomeService;
    private readonly DatabaseSelectorService _databaseSelectorService;

    public IncomeController(IncomeService incomeService, DatabaseSelectorService databaseSelectorService)
    {
        _incomeService = incomeService;
        _databaseSelectorService = databaseSelectorService;
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<ResponseModel<GetIncomeData, IError>>> GetIncomes(
        [FromQuery] DateTime from, DateTime to, string? sortBy, int? account, int? category)
    {
        var dbOptions = _databaseSelectorService.GetUserDatabase(User.Identity.Name);

        var expenses = await _incomeService.GetIncomes(from, to, sortBy, account, category, dbOptions);

        return Ok(new ResponseModel<GetIncomeData, IError>()
        {
            Data = new GetIncomeData(expenses)
        });
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<ResponseModel<StatusResponseData, IError>>> AddIncome([FromBody] AddIncomeRequestModel request)
    {
        var dbOptions = _databaseSelectorService.GetUserDatabase(User.Identity.Name);

        await _incomeService.AddIncome(request, dbOptions);

        return Ok(new ResponseModel<StatusResponseData, IError>()
        {
            Data = new StatusResponseData("Income added successfully")
        });
    }

    [HttpGet]
    [Authorize]
    [Route("categories")]
    public async Task<ActionResult<ResponseModel<GetCategoriesData, IError>>> GetCategories()
    {
        var dbOptions = _databaseSelectorService.GetUserDatabase(User.Identity.Name);

        var categories = await _incomeService.GetCategoriesMeow(dbOptions);

        return Ok(new ResponseModel<GetCategoriesData, IError>()
        {
            Data = new GetCategoriesData(categories)
        });
    }
}
