using System.Globalization;

namespace Budget.API.Models;

public class AccountBalanceModel
{
    public string UkrSib { get; set; }
    public string Privat { get; set; }
    public string Cash { get; set; }
    public string Total { get; set; }

    public AccountBalanceModel(double ukrsib, double privat, double cash, double currencyRate, bool inUah)
    {
        CultureInfo culture;
        if (inUah)
        {
            culture = new CultureInfo("uk-UA");
        }
        else
        {
            culture = new CultureInfo("en-US");

            ukrsib *= currencyRate;
            privat *= currencyRate;
            cash *= currencyRate;
        }

        UkrSib = ukrsib.ToString("C", culture);
        Privat = privat.ToString("C", culture);
        Cash = cash.ToString("C", culture);
        Total = (ukrsib + privat + cash).ToString("C", culture);
    }
}
