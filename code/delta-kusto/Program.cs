using CommandLine;
using CommandLine.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

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
                .WithParsed(RunOptions)
                .WithNotParsed(errors => HandleParseError(result, errors));
        }

        private static void RunOptions(CommandLineOptions options)
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