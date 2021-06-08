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
    public class ClientVersionCacheProxy
    {
        #region Inner Types
        private class GitHubRelease
        {
            public string Name { get; set; } = string.Empty;
        }
        #endregion

        private const string TAGS_URL = "https://api.github.com/repos/microsoft/delta-kusto/tags";
        private const string ALL_RELEASES_KEY = "ClientVersionCacheProxy:allReleases";

        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<ClientVersionController> _logger;

        public ClientVersionCacheProxy(
            IMemoryCache memoryCache,
            ILogger<ClientVersionController> logger)
        {
            _memoryCache = memoryCache;
            _logger = logger;
        }

        public async Task<IImmutableList<string>> GetNewestClientVersionsAsync(
            string? fromClientVersion)
        {
            try
            {
                var allReleases = await CacheOrFetchAllAvailableReleasesAsync();

                if (allReleases != null)
                {
                    var versions = FindNewestClientVersions(allReleases, fromClientVersion);

                    return versions;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

            return ImmutableArray<string>.Empty;
        }

        private IImmutableList<string> FindNewestClientVersions(
            IImmutableList<GitHubRelease> allReleases,
            string? fromClientVersionText)
        {
            //  Versions we can parse
            var allVersions = allReleases
                .Select(r => ParseVersion(r.Name))
                .Where(v => v != null)
                .Select(v => v!)
                .OrderByDescending(v => v[0])
                .ThenByDescending(v => v[1])
                .ThenByDescending(v => v[2])
                .ThenByDescending(v => v[3]);
            var search = ParseVersion(fromClientVersionText);
            //  Versions newer than current with same major
            var newerVersionsSameMajor = allVersions
                .Where(v => search[0] == null || v[0] == search[0]);
            //  Versions newer than current with same major & minor
            var newerVersionsSameMajorMinor = newerVersionsSameMajor
                .Where(v => search[1] == null || v[1] == search[1]);
            //  Versions newer than current with same major & minor & build
            var newerVersionsSameMajorMinorBuild = newerVersionsSameMajorMinor
                .Where(v => search[2] == null || v[2] == search[2]);
            var allPossibleNewVersions = new[]
            {
                newerVersionsSameMajor.FirstOrDefault(),
                newerVersionsSameMajorMinor.FirstOrDefault(),
                newerVersionsSameMajorMinorBuild.FirstOrDefault()
            };
            var allActualVersions = allPossibleNewVersions
                .Where(v => v != null)
                .Select(v => v!)
                .Select(v => new Version(v[0]!.Value, v[1]!.Value, v[2]!.Value, v[3]!.Value))
                .Distinct()
                //  After figuring out distincts, order them in increasing order
                .OrderBy(v => v)
                .Select(v => v!.ToString())
                .ToImmutableArray();

            return allActualVersions;
        }

        private static IImmutableList<int?> ParseVersion(string? versionText)
        {
            try
            {
                if (versionText != null)
                {
                    var numbers = versionText
                        .Split('.')
                        .Select(p => (int?)int.Parse(p))
                        .ToImmutableArray();

                    if (numbers.Length == 4)
                    {
                        return numbers;
                    }
                    else if (numbers.Length < 4 && numbers.Length > 0)
                    {
                        var fullVersion = Enumerable
                            .Range(0, 4)
                            .Select(i => i < numbers.Length ? numbers[i] : null)
                            .ToImmutableArray();

                        return fullVersion;
                    }
                }
            }
            catch
            {
            }

            return Enumerable
                .Range(0, 4)
                .Select(i => (int?)null)
                .ToImmutableArray();
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