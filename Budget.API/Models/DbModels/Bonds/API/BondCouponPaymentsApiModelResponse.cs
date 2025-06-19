namespace Budget.API.Models.DbModels.Bonds.API;

public class BondCouponPaymentsApiModelResponse
{
    public List<BondCouponPaymentsApiModel> data { get; set; }
}

public class BondCouponPaymentsApiModel
{
    public string dat { get; set; }
    public string kupon { get; set; }
    public string payment { get; set; }
}
