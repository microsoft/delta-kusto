using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DeltaKustoApi.Controllers.ClientVersion
{
    [ApiController]
    [Route("[controller]")]
    public class ClientVersionController : ControllerBase
    {
        #region Inner Types
        private class GitHubRelease
        {
            public string Name { get; set; } = string.Empty;
        }
        #endregion

        private const string TAGS_URL = "https://api.github.com/repos/microsoft/delta-kusto/tags";
        private const string ALL_RELEASES_KEY = "ClientVersionController:allReleases";

        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<ClientVersionController> _logger;
        private readonly TelemetryWriter _telemetryWriter;

        public ClientVersionController(
            IMemoryCache memoryCache,
            ILogger<ClientVersionController> logger,
            TelemetryWriter telemetryWriter)
        {
            _memoryCache = memoryCache;
            _logger = logger;
            _telemetryWriter = telemetryWriter;
        }

        public async Task<ClientVersionOutput> GetAsync(
            [FromQuery]
            string? currentClientVersion)
        {
            try
            {
                _telemetryWriter.PostTelemetry(
                    $"clientVersion:  {currentClientVersion}",
                    Request);

                var allReleases = await CacheOrFetchAllAvailableReleasesAsync();

                if (allReleases != null)
                {
                    var newestVersions = FindNewestClientVersions(
                        allReleases,
                        currentClientVersion);

                    return new ClientVersionOutput
                    {
                        Versions = newestVersions
                    };
                }
                else
                {
                    return new ClientVersionOutput();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);

                return new ClientVersionOutput();
            }
        }

        private IImmutableList<string> FindNewestClientVersions(
            IImmutableList<GitHubRelease> allReleases,
            string? currentClientVersionText)
        {
            //  Versions we can parse
            var allVersions = allReleases
                .Select(r => ParseVersion(r.Name))
                .Where(v => v != null)
                .Select(v => v!);
            var currentClientVersion = ParseVersion(currentClientVersionText);

            if (currentClientVersion != null)
            {
                //  Versions newer than current
                var newerVersions = allVersions
                    .Where(v => v > currentClientVersion)
                    .OrderBy(v => v);
                //  Versions newer than current with same major
                var newerVersionsSameMajor = newerVersions
                    .Where(v => v.Major == currentClientVersion.Major);
                //  Versions newer than current with same major & minor
                var newerVersionsSameMajorMinor = newerVersionsSameMajor
                    .Where(v => v.Minor == currentClientVersion.Minor);
                //  Versions newer than current with same major & minor & build
                var newerVersionsSameMajorMinorBuild = newerVersionsSameMajorMinor
                    .Where(v => v.Build == currentClientVersion.Build);
                var allPossibleNewVersions = new[]
                {
                        newerVersionsSameMajor.FirstOrDefault(),
                        newerVersionsSameMajorMinor.FirstOrDefault(),
                        newerVersionsSameMajorMinorBuild.FirstOrDefault(),
                        newerVersions.FirstOrDefault()
                    };
                var allActualVersions = allPossibleNewVersions
                    .Where(v => v != null)
                    .Distinct()
                    .Select(v => v!.ToString())
                    .ToImmutableArray();

                return allActualVersions;
            }
            else
            {
                var latestVersions = new Version?[] { allVersions.Max() }
                    .Where(v => v != null)
                    .Select(v => v!.ToString())
                    .ToImmutableArray();

                return latestVersions;
            }
        }

        private static Version? ParseVersion(string? versionText)
        {
            try
            {
                if (versionText != null)
                {
                    var version = new Version(versionText);

                    return version;
                }
            }
            catch
            {
            }
         
            return null;
        }

        private async Task<IImmutableList<GitHubRelease>?> CacheOrFetchAllAvailableReleasesAsync()
        {
            var allReleases = _memoryCache.Get(ALL_RELEASES_KEY) as IImmutableList<GitHubRelease>;

            if (allReleases == null)
            {
                allReleases = await FetchAllAvailableReleasesAsync();

                if (allReleases != null)
                {
                    _memoryCache.Set(ALL_RELEASES_KEY, allReleases, TimeSpan.FromMinutes(15));
                }
            }

            return allReleases;
        }

        private async Task<IImmutableList<GitHubRelease>?> FetchAllAvailableReleasesAsync()
        {   //  Tip taken from https://stackoverflow.com/questions/18995854/how-can-i-use-github-api-to-get-all-tags-or-releases-for-a-project
            using (var client = new HttpClient())
            {
                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

                client.DefaultRequestHeaders.UserAgent.Add(
                    new ProductInfoHeaderValue("delta-kusto-api", null));

                var response = await client.GetAsync(
                    TAGS_URL,
                    cts.Token);
                var responseText =
                    await response.Content.ReadAsStringAsync(cts.Token);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var output = JsonSerializer.Deserialize<GitHubRelease[]>(
                        responseText,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                    return output!.ToImmutableArray();
                }
                else
                {
                    return null;
                }
            }
        }
    }
}
