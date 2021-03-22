namespace DeltaKustoApi.Controllers.Activation
{
    public class ActivationOutput
    {
        public ApiInfo ApiInfo { get; set; }
        
        public string HighestAvailableClientVersion { get; set; }

        public bool ClientVersionIsSupported { get; set; }
    }
}