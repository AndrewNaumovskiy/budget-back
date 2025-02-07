using Budget.API.Models;
using Budget.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

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
        [Authorize]
        [Route("accounts")]
        public async Task<ActionResult<ResponseModel<GetAccountsData, IError>>> GetAccounts()
        {
            var accounts = await _balanceService.GetAccounts();

            return Ok(new ResponseModel<GetAccountsData, IError>()
            {
                Data = new GetAccountsData(accounts)
            });
        }

        // TODO: move to statistics controller
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
