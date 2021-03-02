using CommandLine;
using CommandLine.Text;
using DeltaKustoIntegration;
using DeltaKustoLib;
using Kusto.Language.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace delta_kusto
{
    class Program
    {
        static int Main(string[] args)
        {
            //  Use CommandLineParser NuGet package to parse command line
            //  See https://github.com/commandlineparser/commandline
            var parser = new Parser(with =>
            {
                with.HelpWriter = null;
            });

            try
            {
                var result = parser.ParseArguments<CommandLineOptions>(args);

                result
                    .WithNotParsed(errors => HandleParseError(result, errors))
                    .WithParsedAsync(RunOptions);

                return result.Tag == ParserResultType.Parsed
                    ? 0
                    : 1;
            }
            catch (DeltaException ex)
            {
                Console.Error.WriteLine($"Error:  {ex.Message}");
                if (!string.IsNullOrWhiteSpace(ex.Script))
                {
                    Console.Error.WriteLine($"Error:  {ex.Script}");
                }

                return 1;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Exception encountered:  {ex.GetType().FullName} ; {ex.Message}");

                return 1;
            }
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

            //  Dependency injection
            var orchestration = new DeltaOrchestration(
                new FileGateway(),
                new KustoManagementGatewayFactory());

            await orchestration.ComputeDeltaAsync(options.ParameterFilePath);
        }

        private static void HandleParseError(
            ParserResult<CommandLineOptions> result,
            IEnumerable<Error> errors)
        {
            //if (errors.IsVersion() || errors.IsHelp())
            var helpText = HelpText.AutoBuild(result, h =>
            {
                h.AutoVersion = false;
                h.Copyright = string.Empty;

                return HelpText.DefaultParsingErrorsHandler(result, h);
            }, example => example);

            Console.WriteLine(helpText);
        }
    }
}