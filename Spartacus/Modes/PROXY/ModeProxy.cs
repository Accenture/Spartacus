using Spartacus.Properties;
using Spartacus.Spartacus;
using Spartacus.Spartacus.CommandLine;
using Spartacus.Spartacus.Models;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Spartacus.Spartacus.Models.FunctionSignature;

namespace Spartacus.Modes.PROXY
{
    class ModeProxy : ModeBase
    {
        public override void Run()
        {
            switch (RuntimeData.Action.ToLower())
            {
                case "prototype":
                case "prototypes":
                    PrototypeDatabaseGeneration prototypeGenerator = new();
                    prototypeGenerator.Run();
                    break;
                case "exports":
                    ExportsGeneration exportsGeneration = new();
                    exportsGeneration.Run();
                    break;
                default:
                    ProxyGeneration proxyGenerator = new();
                    proxyGenerator.Run();
                    break;
            }
        }

        public override void SanitiseAndValidateRuntimeData()
        {
            switch (RuntimeData.Action.ToLower())
            {
                case "prototypes":
                case "prototype":
                    SanitisePrototypeGenerationRuntimeData();
                    break;
                case "exports":
                    SanitiseExportsGenerationRuntimeData();
                    break;
                default:
                    SanitiseProxyGenerationRuntimeData();
                    break;
            }
        }

        protected void SanitiseExportsGenerationRuntimeData()
        {
            if (RuntimeData.BatchDLLFiles.Count == 0)
            {
                throw new Exception("--dll is missing");
            }
            else
            {
                foreach (string dllFile in RuntimeData.BatchDLLFiles)
                {
                    if (!File.Exists(dllFile))
                    {
                        throw new Exception("--dll file does not exist: " + dllFile);
                    }
                }
            }

            // Check for prototypes path.
            if (!String.IsNullOrEmpty(RuntimeData.PrototypesFile))
            {
                if (!File.Exists(RuntimeData.PrototypesFile))
                {
                    throw new Exception("--prototypes file does not exist: " + RuntimeData.PrototypesFile);
                }
            }
        }

        protected void SanitisePrototypeGenerationRuntimeData()
        {
            // Check for input file where we'll look for header files.
            if (String.IsNullOrEmpty(RuntimeData.Path))
            {
                throw new Exception("--path is missing");
            }
            else if (!Directory.Exists(RuntimeData.Path))
            {
                throw new Exception("--path does not exist: " +  RuntimeData.Path);
            }

            // Check for CSV output file.
            if (String.IsNullOrEmpty(RuntimeData.CSVFile))
            {
                throw new Exception("--csv is missing");
            }
            else if (File.Exists(RuntimeData.CSVFile))
            {
                Logger.Debug("--csv exists and will be overwritten");
            }
        }

        protected void SanitiseProxyGenerationRuntimeData()
        {
            if (RuntimeData.BatchDLLFiles.Count == 0)
            {
                throw new Exception("--dll is missing");
            }
            else
            {
                foreach (string dllFile in RuntimeData.BatchDLLFiles)
                {
                    if (!File.Exists(dllFile))
                    {
                        throw new Exception("--dll file does not exist: " + dllFile);
                    }
                }
            }

            // Check for the output solution that will be created.
            if (String.IsNullOrEmpty(RuntimeData.Solution))
            {
                throw new Exception("--solution is missing");
            }
            else if (Directory.Exists(RuntimeData.Solution) && !RuntimeData.Overwrite)
            {
                throw new Exception("--solution already exists and --overwrite has not been passed as an argument");
            }

            // If a ghidra path has been passed, validate it.
            if (!String.IsNullOrEmpty(RuntimeData.GhidraHeadlessPath))
            {
                if (!File.Exists(RuntimeData.GhidraHeadlessPath))
                {
                    throw new Exception("--ghidra file does not exist: " + RuntimeData.GhidraHeadlessPath);
                }
            }

            // Check for functions to proxy.
            if (RuntimeData.FunctionsToProxy.Count > 0)
            {
                RuntimeData.FunctionsToProxy = RuntimeData.FunctionsToProxy
                    .Select(s => s.Trim().ToLower())                                    // Trim and lowercase.
                    .Where(s => !string.IsNullOrWhiteSpace(s))                          // Remove empty
                    .Distinct()                                                         // Remove duplicates
                    .ToList();

                // If after cleaning up, we have no values left, abort.
                if (RuntimeData.FunctionsToProxy.Count == 0)
                {
                    throw new Exception("--only is invalid");
                }
            }

            // Check for prototypes path.
            if (!String.IsNullOrEmpty(RuntimeData.PrototypesFile))
            {
                if (!File.Exists(RuntimeData.PrototypesFile))
                {
                    throw new Exception("--prototypes file does not exist: " + RuntimeData.PrototypesFile);
                }
            }
        }
    }
}
