using System.ComponentModel.DataAnnotations.Schema;

namespace Budget.API.Models.DbModels;

[Table("accounts")]
public class AccountDbModel
{
    public int Id { get; set; }
    public string Name { get; set; }
    public double Balance { get; set; }

    public List<TransactionDbModel> Transactions { get; set; } = [];
}
