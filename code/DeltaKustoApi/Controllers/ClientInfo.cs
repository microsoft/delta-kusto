using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DeltaKustoApi.Controllers
{
    public class ClientInfo
    {
        public string ClientVersion { get; set; } = "";

        public string OS { get; set; } = "";
        
        public string OsVersion { get; set; } = "";
    }
}