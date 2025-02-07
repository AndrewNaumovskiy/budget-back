using Budget.API.Models;
using Budget.API.Services;
using Budget.API.Models.RequestModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Budget.API.Controllers;

[Route("apibudget/transactions")]
[ApiController]
public class TransactionsController : ControllerBase
{
    private readonly TransactionsService _transactionService;
    private readonly DatabaseSelectorService _databaseSelectorService;

    public TransactionsController(TransactionsService transactionService, DatabaseSelectorService databaseSelectorService)
    {
        _transactionService = transactionService;
        _databaseSelectorService = databaseSelectorService;
    }

    [HttpGet]
    [Authorize]
    [Route("{id:int}")]
    public async Task<ActionResult<ResponseModel<GetTransactionData, IError>>> GetTransaction(int id)
    {
        var dbOptions = _databaseSelectorService.GetUserDatabase(User.Identity.Name);

        var transaction = await _transactionService.GetTransaction(id, dbOptions);

        if(transaction == null)
            return NotFound(new ResponseModel<IData, Error>()
            {
                Error = new Error("Transaction not found")
            });

        return Ok(new ResponseModel<GetTransactionData, IError>()
        {
            Data = new GetTransactionData(transaction)
        });
    }

    [HttpPut]
    [Authorize]
    [Route("{id:int}")]
    public async Task<ActionResult<ResponseModel<StatusResponseData, IError>>> GetTransaction(int id, [FromBody] EditTransactionRequestModel transaction)
    {
        var dbOptions = _databaseSelectorService.GetUserDatabase(User.Identity.Name);

        var temp = await _transactionService.EditTransaction(id, transaction, dbOptions);

        if(temp == false)
            return NotFound(new ResponseModel<IData, Error>()
            {
                Error = new Error("Transaction not found")
            });

        return Ok(new ResponseModel<StatusResponseData, IError>()
        {
            Data = new StatusResponseData("Transaction edited successfully")
        });
    }

    [HttpDelete]
    [Authorize]
    [Route("{id:int}")]
    public async Task<ActionResult<ResponseModel<StatusResponseData, IError>>> DeleteTransaction(int id)
    {
        var dbOptions = _databaseSelectorService.GetUserDatabase(User.Identity.Name);

        var temp = await _transactionService.DeleteTransaction(id, dbOptions);

        if (temp == false)
            return NotFound(new ResponseModel<IData, Error>()
            {
                Error = new Error("Transaction not found")
            });

        return Ok(new ResponseModel<StatusResponseData, IError>()
        {
            Data = new StatusResponseData("Transaction deleted successfully")
        });
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<ResponseModel<GetRecentTransactionsData, IError>>> GetRecentTransactions([FromQuery] string? page)
    {
        var dbOptions = _databaseSelectorService.GetUserDatabase(User.Identity.Name);

        var recent = await _transactionService.GetRecentTransactions(page, dbOptions);

        return Ok(new ResponseModel<GetRecentTransactionsData, IError>()
        {
            Data = new GetRecentTransactionsData(recent)
        });
    }

    [HttpGet]
    [Authorize]
    [Route("summary")]
    public async Task<ActionResult<ResponseModel<GetSummaryData, IError>>> GetSummary([FromQuery] string? year, string? month)
    {
        var dbOptions = _databaseSelectorService.GetUserDatabase(User.Identity.Name);

        var (income, expenses, savings, unspecified) = await _transactionService.GetSummary(year, month, dbOptions);

        return Ok(new ResponseModel<GetSummaryData, IError>()
        {
            Data = new GetSummaryData(income, expenses, savings, unspecified)
        });
    }

}
