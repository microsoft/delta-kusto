using CommandLine;
using CommandLine.Text;
using DeltaKustoIntegration;
using DeltaKustoLib;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace delta_kusto
{
    internal class Program
    {
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
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

            //  Use CommandLineParser NuGet package to parse command line
            //  See https://github.com/commandlineparser/commandline
            var parser = new Parser(with =>
            {
                with.HelpWriter = null;
            });
            var sessionId = Guid.NewGuid().ToString();

            Console.WriteLine();
            Console.WriteLine($"delta-kusto {AssemblyVersion}");
            Console.WriteLine($"Session:  {sessionId}");
            Console.WriteLine();

            try
            {
                var result = parser.ParseArguments(() => new CommandLineOptions(), args);

                await result
                    .WithNotParsed(errors => HandleParseError(result, errors))
                    .WithParsedAsync(async options => await RunOptionsAsync(options, sessionId));

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
            finally
            {
                Console.Out.Flush();
            }
        }

        private static void DisplayGenericException(Exception ex, string tab = "")
        {
            Console.Error.WriteLine($"{tab}Exception encountered:  {ex.GetType().FullName} ; {ex.Message}");
            Console.Error.WriteLine($"{tab}Stack trace:  {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                DisplayGenericException(ex.InnerException, tab + "  ");
            }
        }

        private static void DisplayDeltaException(DeltaException ex, string tab = "")
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

        private static async Task RunOptionsAsync(CommandLineOptions options, string sessionId)
        {
            if (options.Verbose!.Value)
            {
                Console.WriteLine("Verbose output enabled");
            }

            //  Dependency injection
            var tracer = new ConsoleTracer(options.Verbose!.Value);
            var apiClient = new ApiClient(tracer, new SimpleHttpClientFactory(tracer));
            var orchestration = new DeltaOrchestration(
                tracer,
                apiClient);
            var success = await orchestration.ComputeDeltaAsync(
                options.ParameterFilePath!,
                options.Overrides!,
                sessionId);

            if (!success)
            {
                throw new DeltaException("Failure due to drop commands");
            }
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
                h.Heading = string.Empty;

                return HelpText.DefaultParsingErrorsHandler(result, h);
            }, example => example);

            Console.WriteLine(helpText);
        }
    }
}