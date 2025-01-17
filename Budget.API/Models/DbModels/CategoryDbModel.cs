using System.ComponentModel.DataAnnotations.Schema;

namespace Budget.API.Models.DbModels;

[Table("categories")]
public class CategoryDbModel
{
    public int Id { get; set; }
    public string Name { get; set; }

    public List<SubCategoryDbModel> SubCategories { get; set; } = [];
}
