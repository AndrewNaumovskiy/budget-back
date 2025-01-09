using System.ComponentModel.DataAnnotations.Schema;

namespace Budget.API.Models.DbModels;

[Table("expenses")]
public class ExpenseDbModel
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public double Amount { get; set; }
    public double Currency { get; set; }
    public int AccountId { get; set; }
    public int CategoryId { get; set; }
    public string Desc { get; set; }

    public AccountDbModel Account { get; set; }
    public CategoryDbModel Category { get; set; }
}
