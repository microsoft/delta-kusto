using DeltaKustoLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DeltaKustoIntegration.Parameterization
{
    public class MainParameterization
    {
        public string Schema { get; set; } = "Temp-URI!";

        public bool SendTelemetryOptIn { get; set; } = false;

        public TokenProviderParameterization? TokenProvider { get; set; }

        public IDictionary<string, JobParameterization> Jobs { get; set; } = new Dictionary<string, JobParameterization>();

        public void Validate()
        {
            if (Jobs.Count == 0)
            {
                throw new DeltaException("'jobs' must contain at least one job");
            }
            foreach (var pair in Jobs)
            {
                var (key, job) = pair;

                try
                {
                    job.Validate();
                }
                catch (DeltaException ex)
                {
                    throw new DeltaException($"Issue with job '{key}' parameters", ex);
                }
            }

            var clusterJobs = Jobs
                .Values
                .Where(j => (j.Current?.Database != null) || (j.Target?.Database != null));

            if (clusterJobs.Any() && TokenProvider == null)
            {
                throw new DeltaException(
                    "At least one job requires a connection to a cluster "
                    + "; 'tokenProvider' must be populated");
            }
            if (TokenProvider != null)
            {
                TokenProvider.Validate();
            }
        }
    }
}