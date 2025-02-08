using Budget.API.Models.DbModels;
using Budget.API.Models.Dtos;

namespace Budget.API.Models;

public class ResponseModel<D, E>
        where D : IData
        where E : IError
{
    public D Data { get; set; }
    public E Error { get; set; }
}

public interface IError { }

public class Error : IError
{
    public string Description { get; set; }
    public Error(string desc)
    {
        Description = desc;
    }
}

public interface IData { }

public class StatusResponseData : IData
{
    public string Status { get; set; }
    public StatusResponseData(string status)
    {
        Status = status;
    }
}

public class GetExpensesData : IData
{
    public List<TransactionDto> Expenses { get; set; }
    public GetExpensesData(List<TransactionDto> expenses)
    {
        Expenses = expenses;
    }
}
public class GetCategoriesData : IData
{
    public List<CategoryDto> Categories { get; set; }
    public GetCategoriesData(List<CategoryDto> categories)
    {
        Categories = categories;
    }
}



public class GetAccountsData : IData
{
    public List<AccountDto> Accounts { get; set; }
    public GetAccountsData(List<AccountDto> accounts)
    {
        Accounts = accounts;
    }
}
public class GetRecentTransactionsData : IData
{
    public List<TransactionDto> Transactions { get; set; }
    public GetRecentTransactionsData(List<TransactionDto> transactions)
    {
        Transactions = transactions;
    }
}
public class GetIncomeExpenseChartData : IData
{
    public List<double> Income { get; set; }
    public List<double> Expense { get; set; }
    public GetIncomeExpenseChartData(List<double> income, List<double> expenses)
    {
        Income = income;
        Expense = expenses;
    }
}

public class GetIncomeData : IData
{
    public List<TransactionDto> Income { get; set; }
    public GetIncomeData(List<TransactionDto> income)
    {
        Income = income;
    }
}


public class GetTransactionData : IData
{
    public TransactionDto Transaction { get; set; }
    public GetTransactionData(TransactionDto transaction)
    {
        Transaction = transaction;
    }
}
public class GetSummaryData : IData
{
    public double Income { get; set; }
    public double Expenses { get; set; }
    public double Savings { get; set; }
    public double Unspecified { get; set; }
    public GetSummaryData(double income, double expenses, double savings, double unspec)
    {
        Income = income;
        Expenses = expenses;
        Savings = savings;
        Unspecified = unspec;
    }
}

public class LoginData : IData
{
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
    public LoginData(string accessToken, string refreshToken)
    {
        AccessToken = accessToken;
        RefreshToken = refreshToken;
    }
}