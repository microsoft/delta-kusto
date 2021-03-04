using DeltaKustoLib;
using Kusto.Language.Syntax;
using System;
using System.Linq;

namespace DeltaKustoIntegration.Parameterization
{
    public class TokenProviderParameterization
    {
        public TokenMapParameterization[]? TokenMap { get; set; }

        public ServicePrincipalLoginParameterization? Login { get; set; }

        internal void Validate()
        {
            if (TokenMap != null && Login != null)
            {
                throw new DeltaException(
                    "Both 'tokenMap' and 'login' can't both be populated in token provider");
            }
            if (TokenMap == null && Login == null)
            {
                throw new DeltaException(
                    "Either 'tokenMap' or 'login' must be populated in a source");
            }
            if (TokenMap != null)
            {
                if (TokenMap.Length == 0)
                {
                    throw new DeltaException(
                        "'tokenMap' can't be empty");
                }
                foreach (var map in TokenMap)
                {
                    map.Validate();
                }

                var duplicateClusterUris = TokenMap
                    .GroupBy(tm => tm.ClusterUri!.ToLower())
                    .Select(g => new { ClusterUri = g.Key, Count = g.Count() })
                    .Where(o => o.Count > 1);

                if (duplicateClusterUris.Any())
                {
                    var duplicateText = string.Join(
                        ", ",
                        duplicateClusterUris.Select(o => $"'{o.ClusterUri}' ({o.Count})"));

                    throw new DeltaException(
                        "The following cluster uris are duplicated in 'tokenMap':  "
                        + duplicateClusterUris);
                }
            }
            if (Login != null)
            {
                Login.Validate();
            }
        }
    }
}