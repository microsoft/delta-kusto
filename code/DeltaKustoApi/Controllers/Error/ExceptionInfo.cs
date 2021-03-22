namespace DeltaKustoApi.Controllers.Error
{
    public class ExceptionInfo
    {
        public string Message { get; set; }

        public string ExceptionType { get; set; }
        
        public string StackTrace { get; set; }
    }
}