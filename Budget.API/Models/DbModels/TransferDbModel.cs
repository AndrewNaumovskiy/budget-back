using System.ComponentModel.DataAnnotations.Schema;

namespace Budget.API.Models.DbModels;

[Table("transfers")]
public class TransferDbModel
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public double Amount { get; set; }
    public int FromAccountId { get; set; }
    public int ToAccountId { get; set; }
    public string Desc { get; set; }
}
