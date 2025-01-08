namespace Budget.API.Models.RequestModels;

public class AddIncomeRequestModel
{
    public DateTime Date { get; set; }
    public string Account { get; set; }
    public float Amount { get; set; }
    public string Description { get; set; }
}
