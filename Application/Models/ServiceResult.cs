namespace BinanceTradingBot.Application.Models
{
    public class ServiceResult<T>
    {
        public T Data { get; }
        public bool IsSuccess { get; }
        public string ErrorMessage { get; }

        private ServiceResult(T data, bool isSuccess, string errorMessage)
        {
            Data = data;
            IsSuccess = isSuccess;
            ErrorMessage = errorMessage;
        }

        public static ServiceResult<T> Success(T data)
        {
            return new ServiceResult<T>(data, true, null);
        }

        public static ServiceResult<T> Failure(string errorMessage)
        {
            return new ServiceResult<T>(default, false, errorMessage);
        }
    }

    public class ServiceResult
    {
        public bool IsSuccess { get; }
        public string ErrorMessage { get; }

        private ServiceResult(bool isSuccess, string errorMessage)
        {
            IsSuccess = isSuccess;
            ErrorMessage = errorMessage;
        }

        public static ServiceResult Success()
        {
            return new ServiceResult(true, null);
        }

        public static ServiceResult Failure(string errorMessage)
        {
            return new ServiceResult(false, errorMessage);
        }
    }
}