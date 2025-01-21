using Budget.API.Models;
using Budget.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Budget.API.Controllers
{
    [Route("apibudget/balance")]
    [ApiController]
    public class BalanceController : ControllerBase
    {
        private readonly BalanceService _balanceService;

        public BalanceController(BalanceService balanceService)
        {
            _balanceService = balanceService;
        }

        [HttpGet]
        [Route("summary")]
        public async Task<ActionResult<ResponseModel<GetSummaryData, IError>>> GetSummary([FromQuery]string? year, string? month)
        {
            var (income, expenses, savings, unspecified) = await _balanceService.GetSummary(year, month);

            return Ok(new ResponseModel<GetSummaryData, IError>()
            {
                Data = new GetSummaryData(income, expenses, savings, unspecified)
            });
        }

        [HttpGet]
        [Route("recentTransactions")]
        public async Task<ActionResult<ResponseModel<GetRecentTransactionsData, IError>>> GetRecentTransactions([FromQuery] string? page)
        {
            var recent = await _balanceService.GetRecentTransactions(page);

            return Ok(new ResponseModel<GetRecentTransactionsData, IError>()
            {
                Data = new GetRecentTransactionsData(recent)
            });
        }

        [HttpGet]
        [Route("incomeExpenseChart")]
        public async Task<ActionResult<ResponseModel<GetIncomeExpenseChartData, IError>>> GetIncomeExpenseChart()
        {
            var (incomeRes, expenseRes) = await _balanceService.GetIncomeExpenseChart();

            return Ok(new ResponseModel<GetIncomeExpenseChartData, IError>()
            {
                Data = new GetIncomeExpenseChartData(incomeRes, expenseRes)
            });
        }
    }
}
