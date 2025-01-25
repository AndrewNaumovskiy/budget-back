namespace Budget.API.Models.RequestModels;

public class AddIncomeRequestModel
{
    public DateTime Date { get; set; }
    public int AccountId { get; set; }
    public int CategoryId { get; set; }
    public double Amount { get; set; }
    public string Description { get; set; }
}
