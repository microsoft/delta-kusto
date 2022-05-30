using DeltaKustoLib;
using Kusto.Language.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DeltaKustoIntegration.Parameterization
{
    public class TokenProviderParameterization
    {
        public Dictionary<string, TokenParameterization>? Tokens { get; set; }

        public ServicePrincipalLoginParameterization? Login { get; set; }

        public bool SystemManagedIdentity { get; set; } = false;

        public UserPromptParameterization? UserPrompt { get; set; }
        
        public AzCliParameterization? AzCli { get; set; }

        public UserManagedIdentityParameterization? UserManagedIdentity { get; set; }

        internal void Validate()
        {
            var tokenProviderCount = (Tokens != null ? 1 : 0)
                + (Login != null ? 1 : 0)
                + (SystemManagedIdentity ? 1 : 0)
                + (UserPrompt != null ? 1 : 0)
                + (UserManagedIdentity != null ? 1 : 0)
                + (AzCli != null ? 1 : 0);

            if (tokenProviderCount > 1)
            {
                throw new DeltaException("Only one token provider can be populated");
            }
            if (tokenProviderCount == 0)
            {
                throw new DeltaException("At least one token provider must be populated");
            }
            if (Tokens != null)
            {
                if (Tokens.Count == 0)
                {
                    throw new DeltaException("'tokens' can't be empty");
                }
                foreach (var map in Tokens.Values)
                {
                    map.Validate();
                }

                var duplicateClusterUris = Tokens
                    .Values
                    .GroupBy(tm => tm.ClusterUri!.ToLower())
                    .Select(g => new { ClusterUri = g.Key, Count = g.Count() })
                    .Where(o => o.Count > 1);

                if (duplicateClusterUris.Any())
                {
                    var duplicateText = string.Join(
                        ", ",
                        duplicateClusterUris.Select(o => $"'{o.ClusterUri}' ({o.Count})"));

                    throw new DeltaException(
                        "The following cluster uris are duplicated in 'tokens':  "
                        + duplicateClusterUris);
                }
            }
            if (Login != null)
            {
                Login.Validate();
            }
            if (UserManagedIdentity != null)
            {
                UserManagedIdentity.Validate();
            }
            if (UserPrompt != null)
            {
                UserPrompt.Validate();
            }
            if (AzCli != null)
            {
                AzCli.Validate();
            }
        }
    }
}