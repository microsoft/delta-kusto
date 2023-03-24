using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace delta_kusto
{
    internal class RequestDescription
    {
        public string? SessionId { get; set; }

        public string? Os { get; set; }

        public string? OsVersion { get; set; }

        public bool? FailIfDataLoss { get; set; }

        public string? TokenProvider { get; set; }

        public List<RequestDescriptionJob>? Jobs { get; set; }
    }

    [JsonSerializable(typeof(RequestDescription))]
    internal partial class RequestDescriptionSerializerContext : JsonSerializerContext
    {
    }
}