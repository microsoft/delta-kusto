using CommandLine;
using System;
using System.Collections.Generic;
using System.Text;

namespace delta_kusto
{
    public class CommandLineOptions
    {
        [Option('v', "verbose", Required = false, HelpText = "Set output to verbose messages.")]
        public bool Verbose { get; set; } = false;

        [Option("NoLogs", Hidden = true)]
        public bool NoLogs { get; set; } = false;

        [Option('p', "parameter", Required = true, HelpText = "Set parameter file path.")]
        public string ParameterFilePath { get; set; } = string.Empty;

        [Option('o', "override", Required = false, HelpText = "Parameter path overrides (list).")]
        public IEnumerable<string> Overrides { get; set; } = Array.Empty<string>();
    }
}