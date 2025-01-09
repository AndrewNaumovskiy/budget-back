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