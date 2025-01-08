namespace Budget.API.Models.RequestModels;

public class AddExpensesRequestModel
{
    public DateTime Date { get; set; }
    public string Category { get; set; }
    public float Amount { get; set; }
    public string Description { get; set; }
}
