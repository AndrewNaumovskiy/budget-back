using System.ComponentModel.DataAnnotations.Schema;
using Telegram.Bot.Types;

namespace Budget.API.Models.DbModels.Admin;

[Table("refresh_tokens")]
public class RefreshTokenDbModel
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Token { get; set; }
    public DateTime Expires { get; set; }
    public DateTime Created { get; set; }

    public UserDbModel User { get; set; }
}
