using Spartacus.Modes.DETECT;
using Spartacus.Modes.DLL;
using Spartacus.Modes.PROXY;
using Spartacus.ProcMon;
using Spartacus.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spartacus.Spartacus.CommandLine
{
    class CommandLineParser
    {
        private readonly string[] RawArguments;

        private Dictionary<string, string> GlobalArguments = new Dictionary<string, string>
        {
            { "mode", "" },
            { "verbose", "switch" },
            { "debug", "switch" },
            { "existing", "switch" },
            { "all", "switch" },
            { "overwrite", "switch" },
            { "pml", "" },
            { "pmc", "" },
            { "procmon", "" },
            { "csv", "" },
            { "dll", "" },
            { "solution", "" },
            { "ghidra", "" },
            { "only", "" },
            { "exports", "" },
        };

        private Dictionary<string, string> Arguments = new Dictionary<string, string>();

        public CommandLineParser(string[] args)
        {
            RawArguments = args;

            Load();
        }

        private void Load()
        {
            Arguments = LoadCommandLine(GlobalArguments);
            Parse(Arguments);
        }

        private Dictionary<string, string> LoadCommandLine(Dictionary<string, string> arguments)
        {
            foreach (string parameter in arguments.Keys.ToList())
            {
                arguments[parameter] = GetArgument($"--{parameter}", arguments[parameter] == "switch");
            }

            // Remove null values.
            return arguments
                .Where(v => (v.Value != null))
                .ToDictionary(v => v.Key, v => v.Value);
        }

        private string GetArgument(string name, bool isSwitch = false)
        {
            string value = null;

            for (int i = 0; i < RawArguments.Length; i++)
            {
                if (RawArguments[i].ToLower() == name.ToLower())
                {
                    if (isSwitch)
                    {
                        // This is a boolean switch, like --verbose, so we just return a non empty value.
                        value = "true";
                    }
                    else
                    {
                        if (i + 1 <= RawArguments.Length)
                        {
                            value = RawArguments[i + 1];
                        }
                    }
                    break;
                }
            }

            return value;
        }

        private void Parse(Dictionary<string, string> arguments)
        {
            foreach (KeyValuePair<string, string> argument in arguments)
            {
                switch (argument.Key.ToLower())
                {
                    case "mode":
                        RuntimeData.Mode = ParseSpartacusMode(argument.Value);
                        break;
                    case "debug":
                        if (argument.Value.ToLower() != "false")
                        {
                            RuntimeData.Debug = (argument.Value.Length > 0);
                            Logger.IsDebug = RuntimeData.Debug;
                        }
                        break;
                    case "verbose":
                        if (argument.Value.ToLower() != "false")
                        {
                            RuntimeData.Verbose = (argument.Value.Length > 0);
                            Logger.IsVerbose = RuntimeData.Verbose;
                        }
                        break;
                    case "pmc":
                        RuntimeData.PMCFile = argument.Value;
                        break;
                    case "pml":
                        RuntimeData.PMLFile = argument.Value;
                        break;
                    case "csv":
                        RuntimeData.CSVFile = argument.Value;
                        break;
                    case "procmon":
                        RuntimeData.ProcMonExecutable = argument.Value;
                        break;
                    case "exports":
                        RuntimeData.ExportsDirectory = argument.Value;
                        break;
                    case "existing":
                        if (argument.Value.ToLower() != "false")
                        {
                            RuntimeData.IsExistingLog = (argument.Value.Length > 0);
                        }
                        break;
                    case "all":
                        if (argument.Value.ToLower() != "false")
                        {
                            RuntimeData.All = (argument.Value.Length > 0);
                        }
                        break;
                    case "overwrite":
                        if (argument.Value.ToLower() != "false")
                        {
                            RuntimeData.Overwrite = (argument.Value.Length > 0);
                        }
                        break;
                    case "dll":
                        RuntimeData.DLLFile = argument.Value;
                        break;
                    case "solution":
                        RuntimeData.Solution = argument.Value;
                        break;
                    case "ghidra":
                        RuntimeData.GhidraHeadlessPath = argument.Value;
                        break;
                    case "only":
                        RuntimeData.FunctionsToProxy = argument.Value.Trim().Split(',').ToList();
                        break;
                    default:
                        throw new Exception("Unknown argument: " + argument.Key);
                }
            }

            // For debug.
            foreach (KeyValuePair<string, string> argument in arguments)
            {
                Logger.Debug(String.Format("Command Line (raw): {0} = {1}", argument.Key, argument.Value));
            }

            SanitiseAndValidateRuntimeData();
        }

        private RuntimeData.SpartacusMode ParseSpartacusMode(string mode)
        {
            return mode.ToLower() switch
            {
                "dll" => RuntimeData.SpartacusMode.DLL,
                "detect" => RuntimeData.SpartacusMode.DETECT,
                "proxy" => RuntimeData.SpartacusMode.PROXY,
                _ => RuntimeData.SpartacusMode.NONE,
            };
        }

        private void SanitiseAndValidateRuntimeData()
        {
            // If Debug is enabled, force-enable Verbose.
            if (RuntimeData.Debug)
            {
                RuntimeData.Verbose = Logger.IsVerbose = Logger.IsDebug = true;
            }

            switch (RuntimeData.Mode)
            {
                case RuntimeData.SpartacusMode.DLL:
                    RuntimeData.ModeObject = new ModeDLL();
                    break;
                case RuntimeData.SpartacusMode.DETECT:
                    RuntimeData.ModeObject = new ModeDetect();
                    break;
                case RuntimeData.SpartacusMode.PROXY:
                    RuntimeData.ModeObject = new ModeProxy();
                    break;
                default:
                    throw new Exception("--mode is not valid");
            }

            RuntimeData.ModeObject.SanitiseAndValidateRuntimeData();
        }
    }
}
