using Budget.API.Models;
using Budget.API.Services;
using Budget.API.Models.RequestModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Budget.API.Controllers;

[Route("apibudget/transfer")]
[ApiController]
public class TransferController : ControllerBase
{
    private readonly TransferService _transferService;
    private readonly DatabaseSelectorService _databaseSelectorService;

    public TransferController(TransferService transferService, DatabaseSelectorService databaseSelectorService)
    {
        _transferService = transferService;
        _databaseSelectorService = databaseSelectorService;
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<ResponseModel<StatusResponseData, IError>>> AddIncome([FromBody] AddTransferRequestModel request)
    {
        var dbOptions = _databaseSelectorService.GetUserDatabase(User.Identity.Name);

        await _transferService.AddTransfer(request, dbOptions);

        return Ok(new ResponseModel<StatusResponseData, IError>()
        {
            Data = new StatusResponseData("Transfer added successfully")
        });
    }
}
