using Spartacus.Modes.DLL;
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

        //private Dictionary<string, string> GlobalArguments = new Dictionary<string, string>
        //{
        //    { "pml", "" },
        //    { "pmc", "" },
        //    { "csv", "" },
        //    { "exe", "" },
        //    { "exports", "" },
        //    { "procmon", "" },
        //    { "proxy-dll-template", "" },
        //    { "existing-log", "switch" },
        //    { "verbose", "switch" },
        //    { "debug", "switch" },
        //    { "all", "switch" },
        //    { "detect", "switch" },
        //    { "generate-proxy", "switch" },
        //    { "ghidra", "" },
        //    { "dll", "" },
        //    { "output-dir", "" },
        //    { "only-proxy", "" },
        //};
        private Dictionary<string, string> GlobalArguments = new Dictionary<string, string>
        {
            { "mode", "" },
            { "verbose", "switch" },
            { "debug", "switch" },
            { "pml", "" },
            { "pmc", "" },
            { "procmon", "" },
            { "csv", "" },
            { "existing", "switch" },
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
                    //case "detect":
                    //    if (argument.Value.ToLower() != "false")
                    //    {
                    //        RuntimeData.DetectProxyingDLLs = (argument.Value.Length > 0);
                    //    }
                    //    break;
                    //case "generate-proxy":
                    //    if (argument.Value.ToLower() != "false")
                    //    {
                    //        RuntimeData.GenerateProxy = (argument.Value.Length > 0);
                    //    }
                    //    break;
                    //case "ghidra":
                    //    RuntimeData.GhidraHeadlessPath = argument.Value;
                    //    break;
                    //case "dll":
                    //    RuntimeData.DLL = argument.Value;
                    //    break;
                    //case "output-dir":
                    //    RuntimeData.OutputDirectory = argument.Value;
                    //    break;
                    //case "only-proxy":
                    //    RuntimeData.OnlyProxy = argument.Value.Trim().Split(',').ToList();
                    //    break;
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
                //"detect" => RuntimeData.SpartacusMode.DETECT,
                //"proxy" => RuntimeData.SpartacusMode.PROXY,
                //"acl" => RuntimeData.SpartacusMode.ACL,
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
                default:
                    throw new Exception("--mode is not valid");
            }

            RuntimeData.ModeObject.SanitiseAndValidateRuntimeData();
        }

        //private void SanitiseExistingLogProcessing()
        //{
        //    // Log file.
        //    if (RuntimeData.ProcMonLogFile == "")
        //    {
        //        throw new Exception("--pml is missing");
        //    }
        //    else if (!File.Exists(RuntimeData.ProcMonLogFile))
        //    {
        //        throw new Exception("--pml file does not exist");
        //    }
        //}

        //private void SanitiseHijackingDetection()
        //{
        //    // Process Monitor.
        //    if (RuntimeData.ProcMonExecutable == "")
        //    {
        //        throw new Exception("--procmon is missing");
        //    }
        //    else if (!File.Exists(RuntimeData.ProcMonExecutable))
        //    {
        //        throw new Exception("ProcMon executable does not exist: " + RuntimeData.ProcMonExecutable);
        //    }

        //    // Log and Config files.
        //    if (RuntimeData.ProcMonConfigFile == "")
        //    {
        //        // If --pmc is not passed we'll need to create it. In this case we must have a --pml parameter.
        //        if (RuntimeData.ProcMonLogFile == "")
        //        {
        //            throw new Exception("--pml is missing");
        //        }
        //        else if (File.Exists(RuntimeData.ProcMonLogFile))
        //        {
        //            // Just a debug statement.
        //            Logger.Debug("--pml exists and will be overwritten");
        //        }
        //    }
        //    else if (!File.Exists(RuntimeData.ProcMonConfigFile))
        //    {
        //        // If --pmc was passed but does not exist, it's invalid.
        //        throw new Exception("--pmc does not exist: " + RuntimeData.ProcMonConfigFile);
        //    }
        //    else
        //    {
        //        // At this point --pmc exists, so we'll have to use that one.
        //        ProcMonPMC pmc = new ProcMonPMC(RuntimeData.ProcMonConfigFile);

        //        // If the PMC file has no logfile/backing file, check to see if --pml has been set.
        //        if (pmc.GetConfiguration().Logfile == "")
        //        {
        //            if (RuntimeData.ProcMonLogFile == "")
        //            {
        //                throw new Exception("The --pmc file that was passed has no log/backing file configured and no --pml file has been passed either. Either setup the backing file in the existing PML file or pass a --pml parameter");
        //            }
        //            // We'll use the --pml argument that was passed.
        //            RuntimeData.InjectBackingFileIntoConfig = true;
        //        }
        //        else
        //        {
        //            // The PM file has a backing file, so we don't need the --pml argument.
        //            RuntimeData.ProcMonLogFile = pmc.GetConfiguration().Logfile;
        //        }
        //    }
        //}

        //private void SanitiseSharedArguments()
        //{
        //    // CSV File.
        //    if (RuntimeData.CsvOutputFile == "")
        //    {
        //        throw new Exception("--csv is missing");
        //    }
        //    else if (File.Exists(RuntimeData.CsvOutputFile))
        //    {
        //        Logger.Debug("--csv exists and will be overwritten");
        //    }
        //    else
        //    {
        //        Logger.Debug("--csv does not exist and will be created");
        //    }

        //    if (RuntimeData.TrackExecutables.Any())
        //    {
        //        Logger.Debug("--exe passed, will track the following executables: " + String.Join(", ", RuntimeData.TrackExecutables.ToArray()));
        //    }

        //    // Exports directory.
        //    if (RuntimeData.ExportsOutputDirectory == "")
        //    {
        //        Logger.Debug("No --exports passed, will skip proxy DLL generation");
        //    }
        //    else if (Directory.Exists(RuntimeData.ExportsOutputDirectory))
        //    {
        //        Logger.Debug("--exports directory already exists");
        //    }
        //    else
        //    {
        //        // Directory does not exist.
        //        Logger.Debug("--exports directory does not exist, creating it now");
        //        // Will throw exception if there's an error.
        //        Directory.CreateDirectory(RuntimeData.ExportsOutputDirectory);
        //    }

        //    // Proxy DLL Template.
        //    if (RuntimeData.ProxyDllTemplate != "")
        //    {
        //        // Check if the file exists.
        //        if (!File.Exists(RuntimeData.ProxyDllTemplate))
        //        {
        //            throw new Exception("--proxy-dll-template file does not exist");
        //        }

        //        // Load the template into the file.
        //        RuntimeData.ProxyDllTemplate = File.ReadAllText(RuntimeData.ProxyDllTemplate);
        //    }
        //    else
        //    {
        //        // Otherwise, load it from the resource.
        //        RuntimeData.ProxyDllTemplate = Resources.ResourceManager.GetString("proxy.dll.cpp");
        //    }

        //    // Argument combination validation.
        //    if (RuntimeData.ProcMonConfigFile != "" && RuntimeData.TrackExecutables.Any())
        //    {
        //        throw new Exception("You cannot use --pmc with --exe");
        //    }
        //}

        //private void SanitiseDllProxyGeneration()
        //{
        //    // Ghidra path.
        //    if (String.IsNullOrEmpty(RuntimeData.GhidraHeadlessPath))
        //    {
        //        throw new Exception("--ghidra is missing");
        //    }
        //    else if (!File.Exists(RuntimeData.GhidraHeadlessPath))
        //    {
        //        throw new Exception("--ghidra file does not exist: " + RuntimeData.GhidraHeadlessPath);
        //    }

        //    // Dll path.
        //    if (String.IsNullOrEmpty(RuntimeData.DLL))
        //    {
        //        throw new Exception("--dll is missing");
        //    }
        //    else if (!File.Exists(RuntimeData.DLL))
        //    {
        //        throw new Exception("--dll file does not exist: " + RuntimeData.DLL);
        //    }

        //    // Output directory.
        //    if (String.IsNullOrEmpty(RuntimeData.OutputDirectory))
        //    {
        //        throw new Exception("--output-dir is missing");
        //    }
        //    else if (Directory.Exists(RuntimeData.OutputDirectory))
        //    {
        //        throw new Exception("--output-dir already exists: " + RuntimeData.OutputDirectory);
        //    }

        //    // Proxy specific functions only.
        //    if (RuntimeData.OnlyProxy.Count > 0)
        //    {
        //        RuntimeData.OnlyProxy = RuntimeData.OnlyProxy
        //            .Select(s => s.Trim().ToLower())                                    // Trim and lowercase.
        //            .Where(s => !string.IsNullOrWhiteSpace(s))                          // Remove empty
        //            .Distinct()                                                         // Remove duplicates
        //            .ToList();

        //        // If after cleaning up, we have no values left, abort.
        //        if (RuntimeData.OnlyProxy.Count == 0)
        //        {
        //            throw new Exception("--only-proxy has invalid values");
        //        }
        //    }
        //}

        //private void SanitiseRuntimeData()
        //{
        //    // If Debug is enabled, force-enable Verbose.
        //    if (RuntimeData.Debug)
        //    {
        //        RuntimeData.Verbose = Logger.IsVerbose = Logger.IsDebug = true;
        //    }

        //    if (RuntimeData.DetectProxyingDLLs)
        //    {
        //        // Not much here yet.
        //    }
        //    else if (RuntimeData.GenerateProxy)
        //    {
        //        SanitiseDllProxyGeneration();
        //    }
        //    else
        //    {
        //        // Now we need to validate everything.
        //        if (RuntimeData.ProcessExistingLog)
        //        {
        //            SanitiseExistingLogProcessing();
        //        }
        //        else
        //        {
        //            SanitiseHijackingDetection();
        //        }

        //        SanitiseSharedArguments();
        //    }
        //}
    }
}
