namespace DeltaKustoApi.Controllers.Activation
{
    public class ActivationOutput
    {
        public string HighestAvailableClientVersion { get; set; }

        public bool ClientVersionIsSupported { get; set; }
    }
}