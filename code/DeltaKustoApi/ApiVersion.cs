using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace DeltaKustoApi
{
    internal static class ApiVersion
    {
        public static string FullVersion
        {
            get
            {
                var versionAttribute = typeof(ApiVersion)
                    .Assembly
                    .GetCustomAttribute<AssemblyInformationalVersionAttribute>();
                var version = versionAttribute == null
                    ? "<VERSION MISSING>"
                    : versionAttribute!.InformationalVersion;

                return version;
            }
        }
    }
}