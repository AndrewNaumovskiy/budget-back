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
        var user = await _authService.AuthenticateUserAsync(body.Username, body.Password);

        if (user == null)
            return Unauthorized(new ResponseModel<IData, Error>()
            {
                Error = new Error("Invalid username or password")
            });

        var accessToken = _authService.GenerateJwtToken(user);
        var refreshToken = await _authService.GenerateRefreshToken(user);

        return Ok(new ResponseModel<LoginData, IError>()
        {
            Data = new LoginData(accessToken, refreshToken)
        });
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<ResponseModel<LoginData, IError>>> Refresh([FromBody] RefreshTokenRequestModel body)
    {
        var user = await _authService.GetUserByRefreshToken(body.RefreshToken);
        
        if (user == null)
            return Unauthorized(new ResponseModel<IData, Error>()
            {
                Error = new Error("Invalid refresh token")
            });

        var accessToken = _authService.GenerateJwtToken(user);
        var refreshToken = await _authService.GenerateRefreshToken(user);

        return Ok(new ResponseModel<LoginData, IError>()
        {
            Data = new LoginData(accessToken, refreshToken)
        });
    }
}
