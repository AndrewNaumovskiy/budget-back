using System.Globalization;

namespace Budget.API.Models.Dtos;

public class ExpenseChartDto
{
    private static Dictionary<string, string> _categoryEmoji = new()
    {
       {"Житло","🏠"},
        {"Їжа","🍔"},
        {"Ресторани/Кафе","🍽️"},
        {"Доставка їжі","🚚"},
        {"Шоколадки/Чіпсікі","🍫"},
        {"Ліки/Аптека","💊"},
        {"Візит до лікаря","🩺"},
        {"Краса","💅"},
        {"Гром. транспорт","🚌"},
        {"Таксі","🚕"},
        {"Інтернет/Зв'язок","🌐"},
        {"Цифрові покупки","💻"},
        {"Сервіси","🔧"},
        {"Кіно/театри/концерти","🎭"},
        {"Спорт і фітнес","🏋️"},
        {"Подорожі","✈️"},
        {"Побутова техніка","🔌"},
        {"Меблі","🛋️"},
        {"Освіта","📚"},
        {"Подарунки","🎁"},
        {"Донати","❤️"},
        {"Допомога сім'ї","👪"},
        {"Одяг/Взуття","👗"},
        {"Хоз. продукти","🧴"},
        {"Банківське обсл.","🏦"},
        {"Книги","📖"},
        {"Інші","🔍"},
    };

    public string Emoji { get; set; }
    public string CategoryName { get; set; }
    public double Amount { get; set; }
    public string Percentage { get; set; }

    public ExpenseChartDto(string subCategoryName, double amount)
    {
        Emoji = _categoryEmoji[subCategoryName];
        CategoryName = subCategoryName;
        Amount = amount;
    }

    public void CalculatePercentage(double total)
    {
        Percentage = $"{Math.Round(Amount * 100 / total, 2)} %";
    }

    public override string ToString()
    {
        return $"{Emoji} *{CategoryName}*: {Amount.ToString("C", CultureInfo.GetCultureInfo("uk-UA"))} ({Percentage})";
    }
}
