namespace Budget.API.Models.DbModels.Bonds.API;

public class GetAllBondsApiModelRequest
{
    public List<GetAllBondsApiModel> data { get; set; }
}

public class GetAllBondsApiModel
{
    public string isin { get; set; }
    public string askPrice { get; set; }
    public string type { get; set; }
    public string dend { get; set; } // buy now
}
