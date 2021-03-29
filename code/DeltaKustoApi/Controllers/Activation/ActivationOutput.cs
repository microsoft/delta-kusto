namespace DeltaKustoApi.Controllers.Activation
{
    public class ActivationOutput
    {
        public ApiInfo ApiInfo { get; set; } = new ApiInfo();

        public string[] HighestAvailableClientVersions { get; set; } = new string[0];
    }
}