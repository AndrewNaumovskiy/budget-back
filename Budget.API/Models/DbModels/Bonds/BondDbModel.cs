using System.ComponentModel.DataAnnotations.Schema;

namespace Budget.API.Models.DbModels.Bonds;

[Table("bonds")]
public class BondDbModel
{
    public int Id { get; set; }
    
    public string Isin { get;set; }

    public DateTime DateEnd { get; set; }

    public decimal Cost { get; set; }

    public int Type { get; set; } // 0 - SIM; 1 - YTM

    public int Amount { get; set; }

    public int IsClosed { get; set; }

    public IList<ActiveDbModel> Actives { get; set; } = [];
    public IList<CostHistoryDbModel> CostHistory { get; set; } = [];
    public IList<PaymentDbModel> Payments { get; set; } = [];
}
