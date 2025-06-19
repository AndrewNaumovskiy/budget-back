using System.ComponentModel.DataAnnotations.Schema;

namespace Budget.API.Models.DbModels.Bonds;

[Table("payments")]
public class PaymentDbModel
{
    public int Id { get; set; }
    public int BondId { get; set; }
    public DateTime Date { get; set; }
    public decimal Payment { get; set; }

    public BondDbModel Bond { get; set; }
}
