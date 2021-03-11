using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DeltaKustoIntegration.TokenProvider
{
    public interface ITokenProvider
    {
        Task<string> GetTokenAsync(string clusterUri);
    }
}