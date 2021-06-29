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
                SoftDelete = softDelete.ToString(),
                Recoverability = recoverability
                ? EnableBoolean.Enabled.ToString()
                : EnableBoolean.Disabled.ToString()
            };
        }

        public string SoftDelete { get; init; } = TimeSpan.FromDays(36500).ToString();

        public string Recoverability { get; init; } = EnableBoolean.Enabled.ToString();

        public TimeSpan GetSoftDelete()
        {
            TimeSpan time;

            if (TimeSpan.TryParse(SoftDelete, out time))
            {
                return time;
            }
            else
            {
                throw new DeltaException(
                    $"Can't parse 'SoftDelete' value '{SoftDelete}'");
            }
        }

        public bool GetRecoverability()
        {
            EnableBoolean flag;

            if (Enum.TryParse<EnableBoolean>(Recoverability, out flag))
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