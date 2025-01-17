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