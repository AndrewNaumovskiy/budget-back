namespace Budget.API.Models.RequestModels;

public class AddTransferRequestModel
{
    public DateTime Date { get; set; }
    public int FromAccountId { get; set; }
    public int ToAccountId { get; set; }
    public double Amount { get; set; }
    public string Description { get; set; }
}
