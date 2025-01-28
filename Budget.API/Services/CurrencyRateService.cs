using System.Text.Json;

namespace Budget.API.Services;

public class CurrencyRateService
{
    private const string Endpoint = "https://api.privatbank.ua/p24api/pubinfo?exchange&coursid=11";

    public async Task<double> GetUsdToUah()
    {
        using (var httpClient = new HttpClient())
        {
            var resp = await httpClient.GetAsync(Endpoint);

            if (!resp.IsSuccessStatusCode)
                return 0.0;

            var content = await resp.Content.ReadAsStringAsync();
            var json = JsonSerializer.Deserialize<List<PrivatBankResponseModel>>(content);
            
            var str = json.FirstOrDefault(x => x.ccy == "USD")!.sale;
            return double.Parse(str.Replace('.',','));
        }
    }
}

public class PrivatBankResponseModel
{
    public string ccy { get; set; }
    public string sale { get; set; }
}
