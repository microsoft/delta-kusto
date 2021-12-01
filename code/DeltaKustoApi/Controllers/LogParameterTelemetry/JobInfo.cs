namespace DeltaKustoApi.Controllers.LogParameterTelemetry
{
    public class JobInfo
    {
        public string Current { get; set; } = string.Empty;

        public string Target { get; set; } = string.Empty;

        public bool? FilePath { get; set; }

        public bool? FolderPath { get; set; }

        public bool? UsePluralForms { get; set; }

        public bool? PushToConsole { get; set; }

        public bool? PushToCurrent { get; set; }
    }
}