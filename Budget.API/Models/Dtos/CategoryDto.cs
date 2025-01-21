namespace Budget.API.Models.Dtos;

public class CategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public List<SubCategoryDto> SubCategories { get; set; }

    public CategoryDto(int id, string name)
    {
        Id = id;
        Name = name;
        SubCategories = new();
    }
}

public class SubCategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; }

    public SubCategoryDto(int id, string name)
    {
        Id = id;
        Name = name;
    }
}
