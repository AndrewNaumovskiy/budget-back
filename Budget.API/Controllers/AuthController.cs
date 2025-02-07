using Budget.API.Models;
using Budget.API.Services;
using Budget.API.Models.RequestModels;
using Microsoft.AspNetCore.Mvc;

namespace Budget.API.Controllers;

[Route("apibudget/auth")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<ActionResult<ResponseModel<LoginData, IError>>> Login([FromBody] LoginRequestModel body)
    {
        var token = await _authService.AuthenticateUserAsync(body.Username, body.Password);

        if (token == null)
            return Unauthorized(new ResponseModel<IData, Error>()
            {
                Error = new Error("Invalid username or password")
            });

        return Ok(new ResponseModel<LoginData, IError>()
        {
            Data = new LoginData(token)
        });
    }
}
