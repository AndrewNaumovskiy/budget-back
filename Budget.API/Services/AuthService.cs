using DotNetEnv;
using System.Text;
using System.Security.Claims;
using System.Security.Cryptography;
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

    public async Task<UserDbModel?> AuthenticateUserAsync(string username, string password)
    {
        UserDbModel? user = null;

        //var temp = HashPassword(password);

        using (var db = await _dbContext.CreateDbContextAsync())
        {
            user = await db.Users.FirstOrDefaultAsync(x => x.Username == username);
            
            if (user == null || !CheckPassword(password, user.PasswordHash))
                return null;
        }

        return user;
    }

    public string GenerateJwtToken(UserDbModel user)
    {
        var key = Encoding.UTF8.GetBytes(Env.GetString("JWT_SINGHING_KEY"));
        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, user.Username) }),
            Expires = DateTime.UtcNow.AddHours(2),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public async Task<string> GenerateRefreshToken(UserDbModel user)
    {
        var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        var tokenRecord = new RefreshTokenDbModel
        {
            UserId = user.Id,
            Token = refreshToken,
            Expires = DateTime.UtcNow.AddDays(7),
            Created = DateTime.UtcNow
        };

        using (var db = await _dbContext.CreateDbContextAsync())
        {
            await db.RefreshTokens.AddAsync(tokenRecord);
            await db.SaveChangesAsync();
        }

        return refreshToken;
    }

    public async Task<UserDbModel> GetUserByRefreshToken(string refreshToken)
    {
        using (var db = await _dbContext.CreateDbContextAsync())
        {
            var tokenRecord = await db.RefreshTokens.Where(x => x.Token == refreshToken && x.Expires > DateTime.UtcNow)
                                                    .OrderByDescending(x => x.Created)
                                                    .Include(x => x.User)
                                                    .FirstOrDefaultAsync();

            if (tokenRecord == null)
                return null;

            return tokenRecord.User;
        }
    }

    private string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    private bool CheckPassword(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }
}
