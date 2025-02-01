namespace Budget.API.Helpers;

public class TransactionType
{
    public const int Income = 0;
    public const int Expense = 1;
    public const int Savings = 2;
    public const int Transfer = 3;
}

public class SortBy
{
    public const string Date = "date";
    public const string Amount = "amount";
}