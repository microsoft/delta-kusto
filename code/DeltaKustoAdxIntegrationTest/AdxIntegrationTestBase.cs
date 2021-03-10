using delta_kusto;
using DeltaKustoFileIntegrationTest;
using DeltaKustoIntegration.Parameterization;
using DeltaKustoLib.CommandModel;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace DeltaKustoAdxIntegrationTest
{
    public abstract class AdxIntegrationTestBase : IntegrationTestBase
    {
        //private readonly string _executablePath;
        private bool _isClean = false;

        protected AdxIntegrationTestBase()
        {
            var currentDb = Environment.GetEnvironmentVariable("deltaKustoCurrentDb");
            var targetDb = Environment.GetEnvironmentVariable("deltaKustoTargetDb");
            var clusterUri = Environment.GetEnvironmentVariable("deltaKustoClusterUri");
            var tenantId = Environment.GetEnvironmentVariable("deltaKustoTenantId");
            var servicePrincipalId = Environment.GetEnvironmentVariable("deltaKustoSpId");
            var servicePrincipalSecret = Environment.GetEnvironmentVariable("deltaKustoSpSecret");

            if (string.IsNullOrWhiteSpace(currentDb))
            {
                throw new ArgumentNullException(nameof(currentDb));
            }
            if (string.IsNullOrWhiteSpace(targetDb))
            {
                throw new ArgumentNullException(nameof(targetDb));
            }
            if (string.IsNullOrWhiteSpace(clusterUri))
            {
                throw new ArgumentNullException(nameof(clusterUri));
            }
            if (string.IsNullOrWhiteSpace(tenantId))
            {
                throw new ArgumentNullException(nameof(tenantId));
            }
            if (string.IsNullOrWhiteSpace(servicePrincipalId))
            {
                throw new ArgumentNullException(nameof(servicePrincipalId));
            }
            if (string.IsNullOrWhiteSpace(servicePrincipalSecret))
            {
                throw new ArgumentNullException(nameof(servicePrincipalSecret));
            }
        }

        protected override async Task<int> RunMainAsync(params string[] args)
        {
            await EnsureCleanAsync();

            return await base.RunMainAsync(args);
        }

        private async Task EnsureCleanAsync()
        {
            if (!_isClean)
            {
                //  Do not clean yet
                await Task.CompletedTask;

                _isClean = true;
            }
        }
    }
}