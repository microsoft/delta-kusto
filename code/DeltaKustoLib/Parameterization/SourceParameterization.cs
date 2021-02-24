namespace DeltaKustoLib.Parameterization
{
    public class SourceParameterization
    {
        public string? ClusterUri { get; set; } = null;
        
        public string[]? FilePaths { get; set; } = null;
        
        public SourceFolderParametrization[]? Folders { get; set; } = null;
    }
}