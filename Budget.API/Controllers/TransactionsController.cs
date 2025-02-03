using Budget.API.Models;
using Budget.API.Services;
using Budget.API.Models.RequestModels;
using Microsoft.AspNetCore.Mvc;

namespace Budget.API.Controllers;

[Route("apibudget/transactions")]
[ApiController]
public class TransactionsController : ControllerBase
{
    private readonly TransactionsService _transactionService;

    public TransactionsController(TransactionsService transactionService)
    {
        _transactionService = transactionService;
    }

    [HttpGet]
    [Route("{id:int}")]
    public async Task<ActionResult<ResponseModel<GetTransactionData, IError>>> GetTransaction(int id)
    {
        var transaction = await _transactionService.GetTransaction(id);

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
    [Route("{id:int}")]
    public async Task<ActionResult<ResponseModel<StatusResponseData, IError>>> GetTransaction(int id, [FromBody] EditTransactionRequestModel transaction)
    {
        var temp = await _transactionService.EditTransaction(id, transaction);

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
    [Route("{id:int}")]
    public async Task<ActionResult<ResponseModel<StatusResponseData, IError>>> DeleteTransaction(int id)
    {
        var temp = await _transactionService.DeleteTransaction(id);

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
    public async Task<ActionResult<ResponseModel<GetRecentTransactionsData, IError>>> GetRecentTransactions([FromQuery] string? page)
    {
        var recent = await _transactionService.GetRecentTransactions(page);

        return Ok(new ResponseModel<GetRecentTransactionsData, IError>()
        {
            Data = new GetRecentTransactionsData(recent)
        });
    }

    [HttpGet]
    [Route("summary")]
    public async Task<ActionResult<ResponseModel<GetSummaryData, IError>>> GetSummary([FromQuery] string? year, string? month)
    {
        var (income, expenses, savings, unspecified) = await _transactionService.GetSummary(year, month);

        return Ok(new ResponseModel<GetSummaryData, IError>()
        {
            Data = new GetSummaryData(income, expenses, savings, unspecified)
        });
    }

}
