using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace DeltaKustoApi.Controllers
{
    public class ApiInfo
    {
        public string ApiVersion { get; set; } = GetAssemblyVersion();

        private static string GetAssemblyVersion()
        {
            var versionAttribute = typeof(ApiInfo)
                .Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            var version = versionAttribute == null
                ? "<VERSION MISSING>"
                : versionAttribute!.InformationalVersion;

            return version;
        }
    }
}