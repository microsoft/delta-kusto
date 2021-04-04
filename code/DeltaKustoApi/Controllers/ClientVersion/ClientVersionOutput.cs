using System.Collections.Immutable;

namespace DeltaKustoApi.Controllers.ClientVersion
{
    public class ClientVersionOutput
    {
        public ApiInfo ApiInfo { get; } = new ApiInfo();

        public IImmutableList<string> Versions { get; set; } = ImmutableArray<string>.Empty;
    }
}