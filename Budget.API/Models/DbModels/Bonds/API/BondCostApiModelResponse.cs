namespace Budget.API.Models.DbModels.Bonds.API;

public class BondCostApiModelResponse
{
    public required List<BondCostApiModel> data { get; set; }
}

public class BondCostApiModel
{
    public string expectedPaymentsAmount { get; set; }
}
