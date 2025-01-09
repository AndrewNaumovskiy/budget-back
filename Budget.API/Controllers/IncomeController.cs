using Budget.API.Models;
using Budget.API.Models.RequestModels;
using Microsoft.AspNetCore.Mvc;

namespace Budget.API.Controllers;

[Route("budget/income")]
[ApiController]
public class IncomeController : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ResponseModel<StatusResponseData, IError>>> AddIncome([FromBody] AddIncomeRequestModel request)
    {


        return Ok(new ResponseModel<StatusResponseData, IError>()
        {
            Data = new StatusResponseData("Income added successfully")
        });
    }
}
