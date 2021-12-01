namespace DeltaKustoApi.Controllers.LogParameterTelemetry
{
    public class LogParameterTelemetryInput
    {
        public string SessionId { get; set; } = string.Empty;

        public bool? SendErrorOptIn { get; set; }

        public bool? FailIfDataLoss { get; set; }

        public string? TokenProvider { get; set; }

        public JobInfo[]? Jobs { get; set; }
    }
}