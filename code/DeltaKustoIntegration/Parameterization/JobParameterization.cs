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
                throw new DeltaException(
                    "Parameter file doesn't contain mandatory 'target' parameter");
            }
            if (Action == null)
            {
                throw new DeltaException(
                    "Parameter file doesn't contain mandatory 'action' parameter");
            }

            try
            {
                Target.Validate();
            }
            catch (DeltaException ex)
            {
                throw new DeltaException("Issue with 'target' source", ex);
            }
            if (Current != null)
            {
                try
                {
                    Current.Validate();
                }
                catch (DeltaException ex)
                {
                    throw new DeltaException("Issue with 'current' source", ex);
                }
            }
            try
            {
                Action.Validate();
            }
            catch (DeltaException ex)
            {
                throw new DeltaException("Issue with 'action'", ex);
            }
            if (Action.PushToCurrent && Current?.Adx == null)
            {
                throw new DeltaException(
                    "'action.pushToCurrent' can only be used "
                    + "in conjonction with 'target.adx'");
            }
        }
    }
}