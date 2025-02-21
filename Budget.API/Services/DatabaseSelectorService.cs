using Budget.API.Helpers;
using Budget.API.Models.DbModels.Admin;
using Microsoft.EntityFrameworkCore;

namespace Budget.API.Services;

public class DatabaseSelectorService
{
    private readonly IDbContextFactory<AdminDbContext> _dbContext;

    private List<UserDbModel> _users;

    private ServerVersion _server;

    public DatabaseSelectorService(IDbContextFactory<AdminDbContext> dbContext)
    {
        _dbContext = dbContext;

        _ = InitService();
    }

    private async Task InitService()
    {
        using (var db = await _dbContext.CreateDbContextAsync())
        {
            _users = await db.Users.AsNoTracking()
                                   .Select(x => new UserDbModel()
                                   {
                                       Id = x.Id,
                                       Username = x.Username,
                                       Database = x.Database,
                                       TelegramId = x.TelegramId
                                   }).ToListAsync();
        }
    }

    public DbContextOptions<BudgetDbContext> GetUserDatabase(string username)
    {
        var db = _users.FirstOrDefault(x => x.Username == username).Database;
        return CreateOptions(db);
    }

    public (string, DbContextOptions<BudgetDbContext>) GetUserDatabase(long telegramId)
    {
        var db = _users.FirstOrDefault(x => x.TelegramId == telegramId);
        return (db.Username, CreateOptions(db.Database));
    }

    public bool CheckUser(long telegramUserId)
    {
        return _users.Any(x => x.TelegramId == telegramUserId);
    }

    private DbContextOptions<BudgetDbContext> CreateOptions(string databaseName)
    {

        var builder = new DbContextOptionsBuilder<BudgetDbContext>();

        if (_server == null)
            _server = ServerVersion.AutoDetect(connString);

        builder.UseMySql(connString, _server, opt =>
        {
            opt.EnableRetryOnFailure(3);
        });

        return builder.Options;
    }
}
