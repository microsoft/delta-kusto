namespace DeltaKustoApi.Controllers.Error
{
    public class ErrorInput
    {
        public ClientInfo ClientInfo { get; set; }

        public string Source { get; set; }
        
        public ExceptionInfo[] Exceptions { get; set; }
    }
}