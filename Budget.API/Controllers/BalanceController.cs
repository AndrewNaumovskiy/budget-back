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
