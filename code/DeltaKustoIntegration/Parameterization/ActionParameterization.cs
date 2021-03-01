namespace DeltaKustoIntegration.Parameterization
{
    public class ActionParameterization
    {
        public string? FilePath { get; set; } = null;

        public string? FolderPath { get; set; }

        public bool? UseTargetCluster { get; set; } = null;
    }
}