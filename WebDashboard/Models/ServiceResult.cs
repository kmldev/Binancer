namespace BinanceTradingBot.WebDashboard.Models
{
    public class ServiceResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool NotFound { get; set; }

        public static ServiceResult Ok(string message = "Opération réussie")
        {
            return new ServiceResult { Success = true, Message = message };
        }

        public static ServiceResult Error(string message, bool notFound = false)
        {
            return new ServiceResult { Success = false, Message = message, NotFound = notFound };
        }
    }

    public class ServiceResult<T> : ServiceResult
    {
        public T? Data { get; set; }

        public static ServiceResult<T> Ok(T data, string message = "Opération réussie")
        {
            return new ServiceResult<T> { Success = true, Message = message, Data = data };
        }

        public static new ServiceResult<T> Error(string message, bool notFound = false)
        {
            return new ServiceResult<T> { Success = false, Message = message, NotFound = notFound };
        }
    }
}