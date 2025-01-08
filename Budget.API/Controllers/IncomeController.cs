using Budget.API.Models.RequestModels;
using Microsoft.AspNetCore.Mvc;

namespace Budget.API.Controllers;

[Route("budget/income")]
[ApiController]
public class IncomeController : ControllerBase
{
    [HttpPost]
    public void Post([FromBody] AddIncomeRequestModel request)
    {

    }
}
