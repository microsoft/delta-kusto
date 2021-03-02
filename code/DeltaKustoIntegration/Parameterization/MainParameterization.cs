using DeltaKustoLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeltaKustoIntegration.Parameterization
{
    public class MainParameterization
    {
        public string Schema { get; set; } = "Temp-URI!";

        public bool SendTelemetryOptIn { get; set; } = false;

        public int? ServicePrincipal { get; set; }

        public JobParameterization[] Jobs { get; set; } = new JobParameterization[0];

        public void Validate()
        {
            if (Jobs.Length == 0)
            {
                throw new DeltaException("'jobs' must contain at least one job");
            }
        }
    }
}