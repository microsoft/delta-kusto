namespace DeltaKustoIntegration.Parameterization
{
    public class SourceFileParametrization
    {
        public string? Database { get; set; }

        public string[]? FilePaths { get; set; } = null;

        public string? FolderPath { get; set; } = null;

        public string[]? Extensions { get; set; } = null;
    }
}