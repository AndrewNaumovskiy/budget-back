using Budget.API.Models;
using Budget.API.Services;
using Budget.API.Models.RequestModels;
using Microsoft.AspNetCore.Mvc;

namespace Budget.API.Controllers;

[Route("apibudget/transfer")]
[ApiController]
public class TransferController : ControllerBase
{
    private readonly TransferService _transferService;

    public TransferController(TransferService transferService)
    {
        _transferService = transferService;
    }

    [HttpPost]
    public async Task<ActionResult<ResponseModel<StatusResponseData, IError>>> AddIncome([FromBody] AddTransferRequestModel request)
    {
        await _transferService.AddTransfer(request);

        return Ok(new ResponseModel<StatusResponseData, IError>()
        {
            Data = new StatusResponseData("Transfer added successfully")
        });
    }
}
