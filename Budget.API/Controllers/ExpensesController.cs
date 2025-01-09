using Budget.API.Models;
using Budget.API.Services;
using Budget.API.Models.RequestModels;
using Microsoft.AspNetCore.Mvc;

namespace Budget.API.Controllers;

[Route("budget/expenses")]
[ApiController]
public class ExpensesController : ControllerBase
{
    private readonly ExpensesService _expensesService;

    public ExpensesController(ExpensesService expensesService)
    {
        _expensesService = expensesService;
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
}
