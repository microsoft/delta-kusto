using DeltaKustoLib;

namespace DeltaKustoIntegration.Parameterization
{
    public class JobParameterization
    {
        public int Priority { get; set; } = int.MaxValue;

        public SourceParameterization? Current { get; set; }

        public SourceParameterization? Target { get; set; }

        public ActionParameterization? Action { get; set; }

        public void Validate()
        {
            if (Target == null)
            {
                throw new DeltaException("Parameter file doesn't contain mandatory 'target' parameters");
            }

            Target.Validate();

            if (Current != null)
            {
                Current.Validate();
            }
            if (Action != null)
            {
                Action.Validate();
                if (Action.UseTargetCluster && Target?.Cluster == null)
                {
                    throw new DeltaException(
                        "'action.useTargetCluster' can only be used "
                        + "in conjonction with 'target.cluster'");
                }
            }
        }
    }
}