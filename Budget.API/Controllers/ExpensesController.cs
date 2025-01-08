using Budget.API.Models.RequestModels;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Budget.API.Controllers;

[Route("budget/expenses")]
[ApiController]
public class ExpensesController : ControllerBase
{
    // GET: api/<ExpensesController>
    [HttpGet]
    public IEnumerable<string> Get()
    {
        return new string[] { "value1", "value2" };
    }

    // GET api/<ExpensesController>/5
    [HttpGet("{id}")]
    public string Get(int id)
    {
        return "value";
    }

    [HttpPost]
    public void Post([FromBody] AddExpensesRequestModel request)
    {

    }

    // PUT api/<ExpensesController>/5
    [HttpPut("{id}")]
    public void Put(int id, [FromBody] string value)
    {
    }

    // DELETE api/<ExpensesController>/5
    [HttpDelete("{id}")]
    public void Delete(int id)
    {
    }
}
