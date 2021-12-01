namespace DeltaKustoApi.Controllers.EndSession
{
    public class EndSessionInput
    {
        public string SessionId { get; set; } = string.Empty;
        
        public bool? IsSuccess { get; set; } = null;
    }
}