using DeltaKustoLib;

namespace DeltaKustoIntegration.Parameterization
{
    public class JobParameterization
    {
        public int Priority { get; set; } = int.MaxValue;

        public bool FailIfDrops { get; set; } = false;

        public SourceParameterization? Current { get; set; }

        public SourceParameterization? Target { get; set; }

        public ActionParameterization? Action { get; set; }

        public void Validate()
        {
            if (Target == null)
            {
                throw new DeltaException(
                    "Parameter file doesn't contain mandatory 'target' parameter");
            }
            if (Action == null)
            {
                throw new DeltaException(
                    "Parameter file doesn't contain mandatory 'action' parameter");
            }

            Target.Validate();

            if (Current != null)
            {
                Current.Validate();
            }
            Action.Validate();
            if (Action.PushToCurrentCluster && Target?.Database == null)
            {
                throw new DeltaException(
                    "'action.useTargetCluster' can only be used "
                    + "in conjonction with 'target.cluster'");
            }
        }
    }
}