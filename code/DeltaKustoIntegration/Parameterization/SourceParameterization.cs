namespace DeltaKustoIntegration.Parameterization
{
    public class SourceParameterization
    {
        public ClusterSourceParameterization? Cluster { get; set; } = null;
        
        public SourceFileParametrization[]? Folders { get; set; } = null;
    }
}