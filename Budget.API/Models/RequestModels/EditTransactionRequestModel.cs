namespace Budget.API.Models.RequestModels;

public class EditTransactionRequestModel
{
    public DateTime Date { get; set; }
    public double Amount { get; set; }
    public double CurrencyRate { get; set; }
    public int AccountId { get; set; }
    public int CategoryId { get; set; }
    public string Description { get; set; }
    public int Type { get; set; }
}
