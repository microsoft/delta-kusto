using CommandLine;
using CommandLine.Text;
using DeltaKustoLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace delta_kusto
{
    internal class Program
    {
        private readonly CancellationToken _ct;

        public Program(CancellationToken ct)
        {
            _ct = ct;
        }

        public static string AssemblyVersion
        {
            get
            {
                var versionAttribute = typeof(Program)
                    .Assembly
                    .GetCustomAttribute<AssemblyInformationalVersionAttribute>();
                var version = versionAttribute == null
                    ? "<VERSION MISSING>"
                    : versionAttribute!.InformationalVersion;

                return version;
            }
        }

        internal static async Task<int> Main(string[] args)
        {
            Console.WriteLine();
            Console.WriteLine($"delta-kusto { AssemblyVersion }");
            Console.WriteLine();

            var tokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(1));
            var ct = tokenSource.Token;
            var programContext = new Program(ct);

            return await programContext.RunAsync(args);
        }

        internal async Task<int> RunAsync(string[] args)
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

                await result
                    .WithNotParsed(errors => HandleParseError(result, errors))
                    .WithParsedAsync(RunOptionsAsync);

                return result.Tag == ParserResultType.Parsed
                    ? 0
                    : 1;
            }
            catch (DeltaException ex)
            {
                DisplayDeltaException(ex);

                return 1;
            }
            catch (Exception ex)
            {
                DisplayGenericException(ex);

                return 1;
            }
        }

        private void DisplayGenericException(Exception ex, string tab = "")
        {
            Console.Error.WriteLine($"{tab}Exception encountered:  {ex.GetType().FullName} ; {ex.Message}");
            if (ex.InnerException != null)
            {
                DisplayGenericException(ex.InnerException, tab + "  ");
            }
        }

        private void DisplayDeltaException(DeltaException ex, string tab = "")
        {
            Console.Error.WriteLine($"{tab}Error:  {ex.Message}");
            if (!string.IsNullOrWhiteSpace(ex.Script))
            {
                Console.Error.WriteLine($"{tab}Error:  {ex.Script}");
            }

            var deltaInnerException = ex.InnerException as DeltaException;

            if (deltaInnerException != null)
            {
                DisplayDeltaException(deltaInnerException, tab + "  ");
            }
            if (ex.InnerException != null)
            {
                DisplayGenericException(ex.InnerException, tab + "  ");
            }
        }

        private async Task RunOptionsAsync(CommandLineOptions options)
        {
            Console.WriteLine("Activating Client...");

            var availableClientVersions = await ApiClient.ActivateAsync(_ct);

            Console.WriteLine("Client Activated");
            if (availableClientVersions != null && availableClientVersions.Any())
            {
                Console.WriteLine(
                    "Newer clients available:  "
                    + string.Join(", ", availableClientVersions));
            }

            if (options.Verbose)
            {
                Console.WriteLine("Verbose output enabled");
            }

            //  Dependency injection
            var orchestration = new DeltaOrchestration(options.Verbose);
            var success = await orchestration.ComputeDeltaAsync(
                options.ParameterFilePath,
                options.Overrides,
                _ct);

            if (!success)
            {
                throw new DeltaException("Failure due to drop commands");
            }
        }

        private void HandleParseError(
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