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
    public List<ExpenseDto> Expenses { get; set; }
    public GetExpensesData(List<ExpenseDto> expenses)
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
public class GetRecentTransactionsData : IData
{
    // TODO: rename model
    public List<ExpenseDto> Transactions { get; set; }
    public GetRecentTransactionsData(List<ExpenseDto> transactions)
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