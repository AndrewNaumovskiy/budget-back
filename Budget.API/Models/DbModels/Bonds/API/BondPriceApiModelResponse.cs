namespace Budget.API.Models.DbModels.Bonds.API;

public class BondPriceApiModelResponse
{
    public required List<BondPriceApiModel> data { get; set; }
}

public class BondPriceApiModel
{
    public required string askPrice { get; set; }
}
