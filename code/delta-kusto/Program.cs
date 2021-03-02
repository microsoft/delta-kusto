using CommandLine;
using CommandLine.Text;
using DeltaKustoIntegration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace delta_kusto
{
    class Program
    {
        static void Main(string[] args)
        {
            //  Use CommandLineParser NuGet package to parse command line
            //  See https://github.com/commandlineparser/commandline
            var parser = new Parser(with =>
            {
                with.HelpWriter = null;
            });
            var result = parser.ParseArguments<CommandLineOptions>(args);

            result
                .WithNotParsed(errors => HandleParseError(result, errors))
                .WithParsedAsync(RunOptions);
        }

        private static async Task RunOptions(CommandLineOptions options)
        {
            var versionAttribute = typeof(Program)
                .Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            var version = versionAttribute == null
                ? "<VERSION MISSING>"
                : versionAttribute!.InformationalVersion;
            Console.WriteLine($"delta-kusto { version }");
            if (options.Verbose)
            {
                Console.WriteLine("Verbose output enabled");
            }

            var orchestration = new DeltaOrchestration(new FileGateway());

            await orchestration.ComputeDeltaAsync(options.ParameterFilePath);
        }

        private static void HandleParseError(
            ParserResult<CommandLineOptions> result,
            IEnumerable<Error> errors)
        {
            //if (errors.IsVersion() || errors.IsHelp())
            var helpText = HelpText.AutoBuild(result, h =>
            {
                h.Copyright = string.Empty;

                return HelpText.DefaultParsingErrorsHandler(result, h);
            }, example => example);

            Console.WriteLine(helpText);
        }
    }
}