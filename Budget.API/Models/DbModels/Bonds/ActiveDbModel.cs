using System.ComponentModel.DataAnnotations.Schema;

namespace Budget.API.Models.DbModels.Bonds;

[Table("actives")]
public class ActiveDbModel
{
    public int Id { get; set; }
    public int BondId { get; set; }
    public DateTime DateBuy { get; set; }
    public decimal Price { get; set; }
    public int Amount { get; set; }
    public int IsClosed { get; set; }
    public decimal Profit { get; set; }

    public BondDbModel Bond { get; set; }
}
