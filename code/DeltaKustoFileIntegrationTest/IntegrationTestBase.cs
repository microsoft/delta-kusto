using delta_kusto;
using DeltaKustoIntegration;
using DeltaKustoIntegration.Kusto;
using DeltaKustoIntegration.Parameterization;
using DeltaKustoLib;
using DeltaKustoLib.CommandModel;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
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

        private static readonly TimeSpan PROCESS_TIMEOUT = TimeSpan.FromSeconds(20);

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
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");

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

            Tracer = new ConsoleTracer(false);

            HttpClientFactory = new SimpleHttpClientFactory(Tracer);
        }

        protected SimpleHttpClientFactory HttpClientFactory { get; }

        protected ITracer Tracer { get; }

        protected async virtual Task<int> RunMainAsync(params string[] args)
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

                    var started = process.Start();

                    if (started)
                    {
                        var ct = new CancellationTokenSource(PROCESS_TIMEOUT).Token;

                        await process.WaitForExitAsync(ct);

                        var output = await process.StandardOutput.ReadToEndAsync();
                        var errors = await process.StandardError.ReadToEndAsync();

                        if (output.Length != 0)
                        {
                            Console.WriteLine("Output:  ");
                            Console.WriteLine(output);
                        }
                        if (errors.Length != 0)
                        {
                            Console.WriteLine("Errors:  ");
                            Console.WriteLine(errors);
                        }

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

        protected async virtual Task RunSuccessfulMainAsync(params string[] args)
        {
            var returnedValue = await RunMainAsync(args);

            Assert.Equal(0, returnedValue);
        }

        protected async virtual Task<MainParameterization> RunParametersAsync(
            string parameterFilePath,
            IEnumerable<(string path, string value)>? overrides = null)
        {
            var pathOverrides = overrides != null
                ? overrides.Select(p => $"{p.path}={p.value}")
                : new string[0];
            var baseParameters = new string[] { "-p", parameterFilePath };
            var cliParameters = overrides != null
                ? baseParameters.Append("-o").Concat(pathOverrides)
                : baseParameters;
            var returnedValue = await RunMainAsync(cliParameters.ToArray());

            if (returnedValue != 0)
            {
                throw new InvalidOperationException($"Main returned {returnedValue}");
            }

            var tracer = new ConsoleTracer(false);
            var apiClient = new ApiClient(tracer, HttpClientFactory);
            var orchestration = new DeltaOrchestration(
                tracer,
                apiClient);
            var parameters = await orchestration.LoadParameterizationAsync(
                parameterFilePath,
                pathOverrides);

            return parameters;
        }

        protected async virtual Task<IImmutableList<CommandBase>> LoadScriptAsync(
            string paramPath,
            string scriptPath)
        {
            var rootFolder = Path.GetDirectoryName(paramPath) ?? "";
            var path = Path.Combine(rootFolder!, scriptPath);

            if (!File.Exists(path))
            {
                throw new InvalidOperationException($"Can't find '{path}'");
            }

            var script = await File.ReadAllTextAsync(path);
            var commands = CommandBase.FromScript(script);

            return commands;
        }
    }
}