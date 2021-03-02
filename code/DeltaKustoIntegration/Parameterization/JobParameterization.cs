using DeltaKustoLib;

namespace DeltaKustoIntegration.Parameterization
{
    public class JobParameterization
    {
        public SourceParameterization? Current { get; set; }

        public SourceParameterization? Target { get; set; }

        public ActionParameterization? Action { get; set; }

        public void Validate()
        {
            if (Current == null)
            {
                throw new DeltaException("Parameter file doesn't contain 'current' parameters");
            }

            Current.Validate();

            if (Target != null)
            {
                Target.Validate();
            }
            if (Action != null)
            {
                Action.Validate();
                if (Action.UseTargetCluster
                    && (Target == null || Target.Cluster == null))
                {
                    throw new DeltaException(
                        "'action.useTargetCluster' can only be used "
                        + "in conjonction with 'target.cluster'");
                }
            }
        }
    }
}