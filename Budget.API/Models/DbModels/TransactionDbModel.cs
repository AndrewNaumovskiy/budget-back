using System.ComponentModel.DataAnnotations.Schema;

namespace Budget.API.Models.DbModels;

[Table("transactions")]
public class TransactionDbModel
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public double Amount { get; set; }
    public double CurrencyRate { get; set; }
    public int AccountId { get; set; }
    public int CategoryId { get; set; }
    public string Desc { get; set; }
    public int Type { get; set; } // 0 - income, 1 - expense, 2 - savings
    public double BalanceAfterTransaction { get; set; }

    public AccountDbModel Account { get; set; }
    public SubCategoryDbModel Category { get; set; }
}
