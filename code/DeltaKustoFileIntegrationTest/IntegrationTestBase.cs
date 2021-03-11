using delta_kusto;
using DeltaKustoIntegration.Parameterization;
using DeltaKustoLib;
using DeltaKustoLib.CommandModel;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace DeltaKustoFileIntegrationTest
{
    public abstract class IntegrationTestBase
    {
        #region Inner Types
        private class MainSettings
        {
            public IDictionary<string, ProjectSetting>? Profiles { get; set; }

            public IDictionary<string, string> GetEnvironmentVariables()
            {
                if (Profiles == null)
                {
                    throw new InvalidOperationException("'profiles' element isn't present in 'launchSettings.json'");
                }
                if (Profiles.Count == 0)
                {
                    throw new InvalidOperationException(
                        "No profile is configured within 'profiles' element isn't present "
                        + "in 'launchSettings.json'");
                }
                var profile = Profiles.First().Value;

                if (profile.EnvironmentVariables == null)
                {
                    throw new InvalidOperationException("'environmentVariables' element isn't present in 'launchSettings.json'");
                }

                return profile.EnvironmentVariables;
            }
        }

        private class ProjectSetting
        {
            public IDictionary<string, string>? EnvironmentVariables { get; set; }
        }
        #endregion

        private readonly string? _executablePath;

        static IntegrationTestBase()
        {
            const string PATH = "Properties\\launchSettings.json";

            if (File.Exists(PATH))
            {
                var settingContent = File.ReadAllText(PATH);
                var mainSetting = JsonSerializer.Deserialize<MainSettings>(
                    settingContent,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                if (mainSetting == null)
                {
                    throw new InvalidOperationException("Can't read 'launchSettings.json'");
                }

                var variables = mainSetting.GetEnvironmentVariables();

                foreach (var variable in variables)
                {
                    Environment.SetEnvironmentVariable(variable.Key, variable.Value);
                }
            }
        }

        protected IntegrationTestBase()
        {
            var path = Environment.GetEnvironmentVariable("deltaKustoSingleExecPath");

            _executablePath = string.IsNullOrWhiteSpace(path)
                ? null
                : path;

            if (_executablePath != null)
            {
                if (!File.Exists(_executablePath))
                {
                    throw new DeltaException(
                        $"Executable file doesn't exist at '{_executablePath}' ; "
                        + $"current directory is:  {Directory.GetCurrentDirectory()}");
                }
            }
        }

        protected async virtual Task<int> RunMainAsync(params string[] args)
        {
            if (_executablePath == null)
            {
                var returnedValue = await Program.Main(args);

                return returnedValue;
            }
            else
            {
                using (var process = new Process())
                {
                    process.StartInfo.FileName = _executablePath;
                    foreach (var arg in args)
                    {
                        process.StartInfo.ArgumentList.Add(arg);
                    }
                    process.OutputDataReceived +=
                        (sender, data) => Console.WriteLine(data.Data);
                    process.ErrorDataReceived +=
                        (sender, data) => Console.WriteLine(data.Data);
                    process.Start();

                    //  Force the exec to execute within 5 seconds
                    var tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));

                    await process.WaitForExitAsync(tokenSource.Token);

                    return process.ExitCode;
                }
            }
        }

        protected async virtual Task RunSuccessfulMainAsync(params string[] args)
        {
            var returnedValue = await RunMainAsync(args);

            Assert.Equal(0, returnedValue);
        }

        protected async virtual Task<MainParameterization> RunParametersAsync(
            string parameterFilePath,
            (string path, object value)[]? overrides = null)
        {
            var jsonOverrides = overrides != null && overrides.Any()
                ? JsonSerializer.Serialize(overrides.Select(o => new { o.path, o.value }))
                : "";
            var returnedValue = await RunMainAsync(
                "-p",
                parameterFilePath,
                "-o",
                jsonOverrides);

            Assert.Equal(0, returnedValue);

            var orchestration = new DeltaOrchestration();
            var parameters = await orchestration.LoadParameterizationAsync(
                parameterFilePath,
                jsonOverrides);

            return parameters;
        }

        protected async virtual Task<IImmutableList<CommandBase>> LoadScriptAsync(string scriptPath)
        {
            var script = await File.ReadAllTextAsync(scriptPath);
            var commands = CommandBase.FromScript(script);

            return commands;
        }
    }
}