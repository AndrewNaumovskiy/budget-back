namespace Budget.API.Models.DbModels.Bonds.API;

public class BondAmountApiModelResponse
{
    public BondAmountApiModel data { get; set; }
}

public class BondAmountApiModel
{
    public bool active { get; set; }
    public string count { get; set; }
}
