using System.ComponentModel.DataAnnotations.Schema;

namespace Budget.API.Models.DbModels;

[Table("subcategories")]
public class SubCategoryDbModel
{
    public int Id { get;set; }
    public string Name { get; set; }
    public int Type { get; set; } // 0 - income, 1 - expense, 2 - savings, 3 - transfer
    public int CategoryId { get; set; }

    public CategoryDbModel UpperCategory { get; set; }
    public List<TransactionDbModel> Transactions { get; set; } = [];
}
