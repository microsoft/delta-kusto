using System.Collections.Immutable;

namespace DeltaKustoApi.Controllers.Activation
{
    public class ActivationOutput
    {
        public ApiInfo ApiInfo { get; set; } = new ApiInfo();

        public string SessionId { get; set; } = string.Empty;
     
        public IImmutableList<string> NewestVersions { get; set; } = ImmutableArray<string>.Empty;
    }
}