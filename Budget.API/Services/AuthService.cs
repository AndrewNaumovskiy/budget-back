using System.Text;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Budget.API.Helpers;
using Budget.API.Models.DbModels.Admin;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Budget.API.Services;

public class AuthService
{
    private readonly IDbContextFactory<AdminDbContext> _dbContext;

    public AuthService(IDbContextFactory<AdminDbContext> dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<string?> AuthenticateUserAsync(string username, string password)
    {
        UserDbModel? user = null;

        //var temp = HashPassword(password);

        using (var db = await _dbContext.CreateDbContextAsync())
        {
            user = await db.Users.FirstOrDefaultAsync(x => x.Username == username);
            
            if (user == null || !CheckPassword(password, user.PasswordHash))
                return null;
        }

        return GenerateJwtToken(user.Username);
    }

    private string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    private bool CheckPassword(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }

    private string GenerateJwtToken(string username)
    {
        var key = Encoding.UTF8.GetBytes("");
        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, username) }),
            Expires = DateTime.UtcNow.AddHours(2),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
