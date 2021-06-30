using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeltaKustoLib.CommandModel.Policies
{
    internal record RetentionPolicy
    {
        public static RetentionPolicy Create(TimeSpan softDelete, bool recoverability)
        {
            return new RetentionPolicy
            {
                SoftDeletePeriod = softDelete.ToString(),
                Recoverability = recoverability
                ? EnableBoolean.Enabled.ToString()
                : EnableBoolean.Disabled.ToString()
            };
        }

        public string SoftDeletePeriod { get; init; } = TimeSpan.FromDays(36500).ToString();

        public string Recoverability { get; init; } = EnableBoolean.Enabled.ToString();

        public TimeSpan GetSoftDeletePeriod()
        {
            TimeSpan time;

            if (TimeSpan.TryParse(SoftDeletePeriod, out time))
            {
                return time;
            }
            else
            {
                throw new DeltaException(
                    $"Can't parse 'SoftDelete' value '{SoftDeletePeriod}'");
            }
        }

        public bool GetRecoverability()
        {
            EnableBoolean flag;

            if (Enum.TryParse<EnableBoolean>(Recoverability, true, out flag))
            {
                return flag == EnableBoolean.Enabled;
            }
            else
            {
                throw new DeltaException(
                    $"Can't parse 'Recoverability' flag value '{Recoverability}'");
            }
        }
    }
}