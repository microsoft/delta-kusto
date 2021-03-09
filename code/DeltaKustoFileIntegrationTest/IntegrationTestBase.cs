using delta_kusto;
using DeltaKustoIntegration.Parameterization;
using DeltaKustoLib.CommandModel;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace DeltaKustoFileIntegrationTest
{
    public abstract class IntegrationTestBase
    {
        private readonly string? _executablePath;

        protected IntegrationTestBase()
        {
            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true);
            var configuration = builder.Build();
            var section = configuration.GetSection("exec-path");
            var path = section.Value;

            _executablePath = string.IsNullOrWhiteSpace(path)
                ? null
                : path;
        }

        protected async Task<int> RunMainAsync(params string[] args)
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

                    await process.WaitForExitAsync();

                    return process.ExitCode;
                }
            }
        }

        protected async Task RunSuccessfulMainAsync(params string[] args)
        {
            var returnedValue = await RunMainAsync(args);

            Assert.Equal(0, returnedValue);
        }

        protected async Task<MainParameterization> RunParametersAsync(string parameterFilePath)
        {
            var returnedValue = await RunMainAsync("-p", parameterFilePath);

            Assert.Equal(0, returnedValue);

            var parameters = await new DeltaOrchestration().LoadParameterizationAsync(parameterFilePath);

            return parameters;
        }

        protected async Task<IImmutableList<CommandBase>> LoadScriptAsync(string scriptPath)
        {
            var script = await File.ReadAllTextAsync(scriptPath);
            var commands = CommandBase.FromScript(script);

            return commands;
        }
    }
}