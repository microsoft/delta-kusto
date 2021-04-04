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

        protected async virtual Task<int> RunMainAsync(
            CancellationToken ct,
            params string[] args)
        {
            if (_executablePath == null)
            {
                Environment.SetEnvironmentVariable("disable-api-calls", "true");

                var returnedValue = await Program.Main(args);

                return returnedValue;
            }
            else
            {
                using (var process = new Process())
                {
                    //  Disable API calls for tests
                    process.StartInfo.EnvironmentVariables.Add("disable-api-calls", "true");
                    process.StartInfo.FileName = _executablePath;
                    foreach (var arg in args)
                    {
                        process.StartInfo.ArgumentList.Add(arg);
                    }
                    process.OutputDataReceived +=
                        (sender, data) => Console.WriteLine(data.Data);
                    process.ErrorDataReceived +=
                        (sender, data) => Console.WriteLine(data.Data);
                    Console.WriteLine($"Start process '{_executablePath}'");

                    var started = process.Start();

                    if (started)
                    {
                        await process.WaitForExitAsync(ct);

                        return process.ExitCode;
                    }
                    else
                    {
                        throw new InvalidProgramException(
                            $"Can't start process '{_executablePath}'");
                    }
                }
            }
        }

        protected async virtual Task RunSuccessfulMainAsync(
            CancellationToken ct,
            params string[] args)
        {
            var returnedValue = await RunMainAsync(ct, args);

            Assert.Equal(0, returnedValue);
        }

        protected async virtual Task<MainParameterization> RunParametersAsync(
            string parameterFilePath,
            CancellationToken ct,
            IEnumerable<(string path, string value)>? overrides = null)
        {
            var pathOverrides = overrides != null
                ? overrides.Select(p => $"{p.path}={p.value}")
                : new string[0];
            var baseParameters = new string[] { "-p", parameterFilePath };
            var cliParameters = overrides != null
                ? baseParameters.Append("-o").Concat(pathOverrides)
                : baseParameters;
            var returnedValue = await RunMainAsync(ct, cliParameters.ToArray());

            if (returnedValue != 0)
            {
                throw new InvalidOperationException($"Main returned {returnedValue}");
            }

            var tracer = new ConsoleTracer(false);
            var orchestration = new DeltaOrchestration(tracer);
            var parameters = await orchestration.LoadParameterizationAsync(
                parameterFilePath,
                pathOverrides);

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