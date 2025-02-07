using System.ComponentModel.DataAnnotations.Schema;

namespace Budget.API.Models.DbModels.Admin;

[Table("users")]
public class UserDbModel
{
    public int Id { get; set; }
    public required string Username { get; set; }
    public required string PasswordHash { get; set; }
    public int TelegramId { get; set; }
}
