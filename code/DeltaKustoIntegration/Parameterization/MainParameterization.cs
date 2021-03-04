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

        public JobParameterization[] Jobs { get; set; } = new JobParameterization[0];

        public void Validate()
        {
            if (Jobs.Length == 0)
            {
                throw new DeltaException("'jobs' must contain at least one job");
            }
            foreach (var job in Jobs)
            {
                job.Validate();
            }

            var clusterJobs = Jobs
                .Where(j => (j.Current != null && j.Current.Cluster != null)
                || (j.Target != null && j.Target.Cluster != null));

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