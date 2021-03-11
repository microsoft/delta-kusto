using delta_kusto;
using DeltaKustoFileIntegrationTest;
using DeltaKustoIntegration.Parameterization;
using DeltaKustoLib.CommandModel;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace DeltaKustoAdxIntegrationTest
{
    public abstract class AdxIntegrationTestBase : IntegrationTestBase
    {
        private readonly bool _overrideCurrentDb;
        private readonly bool _overrideTargetDb;
        private readonly string _clusterUri;
        private readonly string _currentDb;
        private readonly string _targetDb;
        private readonly string _tenantId;
        private readonly string _servicePrincipalId;
        private readonly string _servicePrincipalSecret;
        private bool _isClean = false;

        protected AdxIntegrationTestBase(bool overrideCurrentDb, bool overrideTargetDb)
        {
            _overrideCurrentDb = overrideCurrentDb;
            _overrideTargetDb = overrideTargetDb;

            var clusterUri = Environment.GetEnvironmentVariable("deltaKustoClusterUri");
            var currentDb = Environment.GetEnvironmentVariable("deltaKustoCurrentDb");
            var targetDb = Environment.GetEnvironmentVariable("deltaKustoTargetDb");
            var tenantId = Environment.GetEnvironmentVariable("deltaKustoTenantId");
            var servicePrincipalId = Environment.GetEnvironmentVariable("deltaKustoSpId");
            var servicePrincipalSecret = Environment.GetEnvironmentVariable("deltaKustoSpSecret");

            if (string.IsNullOrWhiteSpace(clusterUri))
            {
                throw new ArgumentNullException(nameof(clusterUri));
            }
            if (string.IsNullOrWhiteSpace(currentDb))
            {
                throw new ArgumentNullException(nameof(currentDb));
            }
            if (string.IsNullOrWhiteSpace(targetDb))
            {
                throw new ArgumentNullException(nameof(targetDb));
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

            _clusterUri = clusterUri;
            _currentDb = currentDb;
            _targetDb = targetDb;
            _tenantId = tenantId;
            _servicePrincipalId = servicePrincipalId;
            _servicePrincipalSecret = servicePrincipalSecret;
        }

        protected override async Task<int> RunMainAsync(params string[] args)
        {
            await EnsureCleanAsync();

            return await base.RunMainAsync(args);
        }

        protected override Task<MainParameterization> RunParametersAsync(
            string parameterFilePath,
            (string path, object value)[]? overrides = null)
        {
            var adjustedOverrides = overrides != null
                ? overrides.ToImmutableList()
                : ImmutableList<(string path, object value)>.Empty;

            adjustedOverrides = adjustedOverrides.Add(
                ("tokenProvider.login.tenantId", _tenantId));
            adjustedOverrides = adjustedOverrides.Add(
                ("tokenProvider.login.clientId", _servicePrincipalId));
            adjustedOverrides = adjustedOverrides.Add(
                ("tokenProvider.login.secret", _servicePrincipalSecret));

            if (_overrideCurrentDb)
            {
                adjustedOverrides = adjustedOverrides.Add(
                    ("jobs.main.current.database.clusterUri", _clusterUri));
                adjustedOverrides = adjustedOverrides.Add(
                    ("jobs.main.current.database.database", _currentDb));
            }
            if (_overrideTargetDb)
            {
                adjustedOverrides = adjustedOverrides.Add(
                    ("jobs.main.target.database.clusterUri", _clusterUri));
                adjustedOverrides = adjustedOverrides.Add(
                    ("jobs.main.target.database.database", _targetDb));
            }

            return base.RunParametersAsync(parameterFilePath, adjustedOverrides.ToArray());
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