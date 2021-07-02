using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeltaKustoLib.CommandModel.Policies
{
    internal record AutoDeletePolicy
    {
        public static AutoDeletePolicy Create(DateTime expiryDate, bool deleteIfNotEmpty)
        {
            return new AutoDeletePolicy
            {
                ExpiryDate = expiryDate.ToString(),
                DeleteIfNotEmpty = deleteIfNotEmpty
            };
        }

        public string ExpiryDate { get; init; } = DateTime.Now.ToString();

        public bool DeleteIfNotEmpty { get; init; } = false;

        public DateTime GetExpiryDate()
        {
            DateTime date;

            if (DateTime.TryParse(ExpiryDate, out date))
            {
                return date;
            }
            else
            {
                throw new DeltaException(
                    $"Can't parse 'ExpiryDate' value '{ExpiryDate}'");
            }
        }
    }
}