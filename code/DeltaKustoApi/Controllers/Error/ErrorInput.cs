namespace DeltaKustoApi.Controllers.Error
{
    public class ErrorInput
    {
        public string SessionId { get; set; } = string.Empty;
        
        public string Source { get; set; } = "";

        public ExceptionInfo[] Exceptions { get; set; } = new ExceptionInfo[0];
    }
}