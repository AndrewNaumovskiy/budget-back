namespace Budget.API.Models.Dtos;

public class TransactionDto
{
    public int Id { get; set; }
    public int Type { get; set; }
    public DateTime Date { get; set; }
    public double Amount { get; set; }
    public string Description { get; set; }
    public string AccountName { get; set; }
    public string CategoryName { get; set; }
    public double Balance { get; set; }
}
