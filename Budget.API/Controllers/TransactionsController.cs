using Budget.API.Models;
using Budget.API.Services;
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
    [Route("{id}")]
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
