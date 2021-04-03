using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DeltaKustoApi.Controllers.Activation
{
    [ApiController]
    [Route("[controller]")]
    public class ActivationController : ControllerBase
    {
        #region Inner Types
        private class GitHubRelease
        {
            public string Name { get; set; } = string.Empty;
        }
        #endregion

        private const string TAGS_URL = "https://api.github.com/repos/microsoft/delta-kusto/tags";

        private readonly ILogger<ActivationController> _logger;
        private readonly TelemetryWriter _telemetryWriter;

        public ActivationController(
            ILogger<ActivationController> logger,
            TelemetryWriter telemetryWriter)
        {
            _logger = logger;
            _telemetryWriter = telemetryWriter;
        }

        public async Task<ActivationOutput> PostAsync(ActivationInput input)
        {
            try
            {
                var newerVersionsTask = FetchNewerClientVersionsAsync(input.ClientInfo.ClientVersion);

                _telemetryWriter.PostTelemetry(input, Request);

                var newerVersions = await newerVersionsTask;

                return new ActivationOutput
                {
                    AvailableClientVersions = newerVersions
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);

                return new ActivationOutput();
            }
        }

        private async Task<string[]> FetchNewerClientVersionsAsync(string currentClientVersionText)
        {
            var currentClientVersion = ParseVersion(currentClientVersionText);

            if (currentClientVersion != null)
            {
                var allReleases = await FetchAllAvailableReleasesAsync();

                if (allReleases != null)
                {
                    //  Versions we can parse
                    var allVersions = allReleases
                        .Select(r => ParseVersion(r.Name))
                        .Where(v => v != null)
                        .Select(v => v!);
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
                        .ToArray();

                    return allActualVersions;
                }
            }

            return new string[0];
        }

        private static Version? ParseVersion(string versionText)
        {
            try
            {
                var version = new Version(versionText);

                return version;
            }
            catch
            {
                return null;
            }
        }

        private async Task<GitHubRelease[]?> FetchAllAvailableReleasesAsync()
        {   //  Tip taken from https://stackoverflow.com/questions/18995854/how-can-i-use-github-api-to-get-all-tags-or-releases-for-a-project
            using (var client = new HttpClient())
            {
                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

                client.DefaultRequestHeaders.UserAgent.Add(
                    new ProductInfoHeaderValue("delta-kusto-api", null));

                var response = await client.GetAsync(
                    TAGS_URL,
                    cts.Token).ConfigureAwait(false);
                var responseText =
                    await response.Content.ReadAsStringAsync(cts.Token).ConfigureAwait(false);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var output = JsonSerializer.Deserialize<GitHubRelease[]>(
                        responseText,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                    return output!;
                }
                else
                {
                    return null;
                }
            }
        }
    }
}
