using CommandLine;
using System;
using System.Collections.Generic;
using System.Text;

namespace delta_kusto
{
    internal class CommandLineOptions
    {
        [Option('v', "verbose", Required = false, HelpText = "Set output to verbose messages.")]
        public bool Verbose { get; set; }

        [Option('p', "parameter", Required = true, HelpText = "Set parameter file path.")]
        public string ParameterFilePath { get; set; } = string.Empty;

        [Option('o', "parameter", Required = false, HelpText = "Parameter JSON overrides.")]
        public string Overrides { get; set; } = string.Empty;
    }
}