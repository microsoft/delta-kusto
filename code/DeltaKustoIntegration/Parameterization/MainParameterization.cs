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

        public bool SendErrorOptIn { get; set; } = false;

        public bool FailIfDataLoss { get; set; } = false;

        public TokenProviderParameterization TokenProvider { get; set; } = new TokenProviderParameterization();

        public Dictionary<string, JobParameterization> Jobs { get; set; } = new Dictionary<string, JobParameterization>();

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
                .Where(j => (j.Current?.Adx != null) || (j.Target?.Adx != null));

            if (clusterJobs.Any())
            {
                TokenProvider.Validate();
            }
        }
    }
}