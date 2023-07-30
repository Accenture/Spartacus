using Spartacus.Modes.COM;
using Spartacus.Modes.DETECT;
using Spartacus.Modes.DLL;
using Spartacus.Modes.PROXY;
using Spartacus.Modes.SIGN;
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

        private Dictionary<string, string> GlobalArguments = new()
        {
            { "mode", "" },
            { "verbose", "switch" },
            { "debug", "switch" },
            { "existing", "switch" },
            { "all", "switch" },
            { "overwrite", "switch" },
            { "external-resources", "switch" },
            { "acl", "switch" },
            { "help", "switch" },
            { "pml", "" },
            { "pmc", "" },
            { "procmon", "" },
            { "csv", "" },
            { "dll", "" },
            { "solution", "" },
            { "ghidra", "" },
            { "only", "" },
            { "action", "" },
            { "path", "" },
            { "prototypes", "" },
            { "pfx", "" },
            { "password", "" },
            { "copy-from", "" },
            { "not-before", "" },
            { "not-after", "" },
            { "subject", "" },
            { "issuer", "" },
            { "algorithm", "" },
            { "timestamp", "" }
        };

        private Dictionary<string, List<string>> Arguments = new();

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

        private Dictionary<string, List<string>> LoadCommandLine(Dictionary<string, string> arguments)
        {
            Dictionary<string, List<string>> data = new();

            foreach (string parameter in arguments.Keys.ToList())
            {
                data[parameter] = GetArgument($"--{parameter}", arguments[parameter] == "switch");
            }

            return data;
        }

        private List<string> GetArgument(string name, bool isSwitch = false)
        {
            List<string> data = new();
            string value = null;

            for (int i = 0; i < RawArguments.Length; i++)
            {
                if (RawArguments[i].ToLower() == name.ToLower())
                {
                    if (isSwitch)
                    {
                        // This is a boolean switch, like --verbose, so we just return a non empty value.
                        value = "true";
                        data.Add(value);
                    }
                    else
                    {
                        if (i + 1 <= RawArguments.Length)
                        {
                            value = RawArguments[i + 1];
                            data.Add(value);
                        }
                    }
                    // We now support multiple params with the same name, so no <break> needed.
                    // break;
                }
            }

            // Remove null values and return.
            return data.Where(d => d != null).ToList();
        }

        private void Parse(Dictionary<string, List<string>> arguments)
        {
            string value = "";
            foreach (KeyValuePair<string, List<string>> argument in arguments)
            {
                if (argument.Value.Count == 0)
                {
                    continue;
                }

                switch (argument.Key.ToLower())
                {
                    case "mode":
                        RuntimeData.Mode = ParseSpartacusMode(argument.Value.First());
                        break;
                    case "debug":
                        if (argument.Value.First().ToLower() != "false")
                        {
                            RuntimeData.Debug = (argument.Value.First().Length > 0);
                            Logger.IsDebug = RuntimeData.Debug;
                        }
                        break;
                    case "verbose":
                        if (argument.Value.First().ToLower() != "false")
                        {
                            RuntimeData.Verbose = (argument.Value.First().Length > 0);
                            Logger.IsVerbose = RuntimeData.Verbose;
                        }
                        break;
                    case "pmc":
                        RuntimeData.PMCFile = argument.Value.First().Trim();
                        break;
                    case "pml":
                        RuntimeData.PMLFile = argument.Value.First().Trim();
                        break;
                    case "csv":
                        RuntimeData.CSVFile = argument.Value.First().Trim();
                        break;
                    case "procmon":
                        RuntimeData.ProcMonExecutable = argument.Value.First();
                        break;
                    case "existing":
                        if (argument.Value.First().ToLower() != "false")
                        {
                            RuntimeData.IsExistingLog = (argument.Value.First().Length > 0);
                        }
                        break;
                    case "all":
                        if (argument.Value.First().ToLower() != "false")
                        {
                            RuntimeData.All = (argument.Value.First().Length > 0);
                        }
                        break;
                    case "overwrite":
                        if (argument.Value.First().ToLower() != "false")
                        {
                            RuntimeData.Overwrite = (argument.Value.First().Length > 0);
                        }
                        break;
                    case "dll":
                        // Here load all --dll properties, currently used only with the PROXY mode.
                        RuntimeData.BatchDLLFiles = argument.Value.Select(v => v.ToLower().Trim()).Distinct().ToList();

                        // If there's only 1 --dll, add it to the original value.
                        if (RuntimeData.BatchDLLFiles.Count == 1)
                        {
                            RuntimeData.DLLFile = RuntimeData.BatchDLLFiles.First();
                        }
                        break;
                    case "solution":
                        RuntimeData.Solution = argument.Value.First().Trim();
                        break;
                    case "ghidra":
                        RuntimeData.GhidraHeadlessPath = argument.Value.First().Trim();
                        break;
                    case "only":
                        RuntimeData.FunctionsToProxy = argument.Value.First().Trim().Split(',').ToList();
                        break;
                    case "external-resources":
                        if (argument.Value.First().ToLower() != "false")
                        {
                            RuntimeData.UseExternalResources = (argument.Value.First().Length > 0);
                        }
                        break;
                    case "acl":
                        if (argument.Value.First().ToLower() != "false")
                        {
                            RuntimeData.isACL = (argument.Value.First().Length > 0);
                        }
                        break;
                    case "help":
                        if (argument.Value.First().ToLower() != "false")
                        {
                            RuntimeData.isHelp = (argument.Value.First().Length > 0);
                        }
                        break;
                    case "action":
                        RuntimeData.Action = argument.Value.First().Trim();
                        break;
                    case "path":
                        RuntimeData.Path = argument.Value.First().Trim();
                        break;
                    case "prototypes":
                        RuntimeData.PrototypesFile = argument.Value.First().Trim();
                        break;
                    case "pfx":
                        RuntimeData.Certificate.PFXFile = argument.Value.First().Trim();
                        break;
                    case "password":
                        RuntimeData.Certificate.Password = argument.Value.First().Trim();
                        break;
                    case "copy-from":
                        RuntimeData.Certificate.CopyFrom = argument.Value.First().Trim();
                        break;
                    case "not-before":
                        try
                        {
                            RuntimeData.Certificate.NotBefore = DateTime.ParseExact(argument.Value.First().Trim(), "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
                        }
                        catch (Exception e)
                        {
                            // Nothing.
                        }
                        break;
                    case "not-after":
                        try
                        {
                            RuntimeData.Certificate.NotAfter = DateTime.ParseExact(argument.Value.First().Trim(), "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
                        }
                        catch (Exception e)
                        {
                            // Nothing.
                        }
                        break;
                    case "subject":
                        RuntimeData.Certificate.Subject = argument.Value.First().Trim();
                        break;
                    case "issuer":
                        RuntimeData.Certificate.Issuer = argument.Value.First().Trim();
                        break;
                    case "algorithm":
                        RuntimeData.Certificate.Algorithm = argument.Value.First().Trim();
                        break;
                    case "timestamp":
                        RuntimeData.Certificate.Timestamp = argument.Value.First().Trim();
                        break;
                    default:
                        throw new Exception("Unknown argument: " + argument.Key);
                }
            }

            // For debug.
            foreach (KeyValuePair<string, List<string>> argument in arguments)
            {
                foreach (string v in argument.Value)
                {
                    Logger.Debug(String.Format("Command Line (raw): {0} = {1}", argument.Key, v));
                }
                
            }

            // If --help has been passed, there's no reason to validate arguments.
            if (!RuntimeData.isHelp)
            {   
                SanitiseAndValidateRuntimeData();
            }
        }

        private RuntimeData.SpartacusMode ParseSpartacusMode(string mode)
        {
            return mode.ToLower() switch
            {
                "dll" => RuntimeData.SpartacusMode.DLL,
                "detect" => RuntimeData.SpartacusMode.DETECT,
                "proxy" => RuntimeData.SpartacusMode.PROXY,
                "com" => RuntimeData.SpartacusMode.COM,
                "sign" => RuntimeData.SpartacusMode.SIGN,
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

            RuntimeData.ModeObject = RuntimeData.Mode switch
            {
                RuntimeData.SpartacusMode.DLL => new ModeDLL(),
                RuntimeData.SpartacusMode.DETECT => new ModeDetect(),
                RuntimeData.SpartacusMode.PROXY => new ModeProxy(),
                RuntimeData.SpartacusMode.COM => new ModeCOM(),
                RuntimeData.SpartacusMode.SIGN => new ModeSign(),
                _ => throw new Exception("--mode is not valid"),
            };
            RuntimeData.ModeObject.SanitiseAndValidateRuntimeData();
        }
    }
}
