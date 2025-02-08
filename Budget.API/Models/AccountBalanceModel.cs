using System.Globalization;
using Budget.API.Models.DbModels;

namespace Budget.API.Models;

public class AccountBalanceModel
{
    public double Amount { get; set; }
    public string Name { get; set; }
    public string Emoji { get; set; }
    public string Balance { get; set; }
    
    private static Dictionary<string, string> _accountEmojii = new()
    {
        { "UkrSib", "💳" },
        { "Privat", "🏧" },
        { "Cash", "💵" },
        { "Total", "📈" },
    };

    public AccountBalanceModel(AccountDbModel dbModel, double currencyRate, bool inUah)
    {
        Amount = dbModel.Balance;
        Name = dbModel.Name;
        Emoji = _accountEmojii[Name];

        CultureInfo culture;
        if (inUah)
        {
            culture = new CultureInfo("uk-UA");
        }
        else
        {
            culture = new CultureInfo("en-US");

            Amount *= currencyRate;
        }

        Balance = Amount.ToString("C", culture);
    }

    public AccountBalanceModel(double sum, bool inUah)
    {
        Amount = sum;
        Name = "Total";
        Emoji = _accountEmojii[Name];

        CultureInfo culture;
        if (inUah)
        {
            culture = new CultureInfo("uk-UA");
        }
        else
        {
            culture = new CultureInfo("en-US");
        }

        Balance = Amount.ToString("C", culture);
    }

    public override string ToString()
    {
        if(Name == "Total")
            return $"{Emoji} *{Name}*: *{Balance}*";

        return $"{Emoji} *{Name}*: {Balance}";
    }
}
