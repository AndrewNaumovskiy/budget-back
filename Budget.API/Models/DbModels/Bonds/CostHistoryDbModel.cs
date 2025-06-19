using System.ComponentModel.DataAnnotations.Schema;

namespace Budget.API.Models.DbModels.Bonds;

[Table("costHistory")]
public class CostHistoryDbModel
{
    public int Id { get; set; }
    public int BondId { get; set; }
    public DateOnly Date { get; set; }
    public decimal Cost { get; set; }

    public BondDbModel Bond { get; set; }
}
