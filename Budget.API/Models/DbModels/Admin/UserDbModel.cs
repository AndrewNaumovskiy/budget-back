using System.ComponentModel.DataAnnotations.Schema;

namespace Budget.API.Models.DbModels.Admin;

[Table("users")]
public class UserDbModel
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string PasswordHash { get; set; }
    public string Database { get; set; }
    public int TelegramId { get; set; }

    public List<RefreshTokenDbModel> RefreshTokens { get; set; }
}
