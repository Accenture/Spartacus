using Spartacus.Properties;
using Spartacus.Spartacus;
using Spartacus.Spartacus.CommandLine;
using Spartacus.Spartacus.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Spartacus.Spartacus.Models.FunctionSignature;
using static Spartacus.Spartacus.PEFileExports;

namespace Spartacus.Modes.PROXY
{
    class ModeProxy : ModeBase
    {
        public override void Run()
        {
            /**
             *  Steps
             *  -----
             *  
             *  1. Get exported functions from input DLL. If there are none, abort.
             *  2. Create a temporary folder where the Ghidra project will be created into.
             *  3. Create a temporary folder where the ExportFunctionDefinitionsINI.java script will be stored in.
             *  4. Extract ExportFunctionDefinitions.java and put it in the path above.
             *  5. Run analyzeHeadless.bat (Ghidra) and get the output ini file. If no file is created, abort.
             *  6. Match exported functions from #1 with the ones from #5, and only keep the ones that have a valid function definition (some fail).
             *     If there are none, abort.
             *  7. For the functions extracted above, generate proxy.def.
             *  8. Extract dllmain.cpp and generate all the proxy functions. Make sure the pragma exports are commented out.
             *  9. Extract proxy.sln and proxy.vcxproj, set them up, and move everything into the output directory.
             *  10. Clean up.
             *  
             */

            Logger.Info("Extracting DLL export functions...");
            List<FileExport> exportedFunctions = Helper.GetExportFunctions(RuntimeData.DLLFile);
            if (exportedFunctions.Count == 0)
            {
                Logger.Error("No export functions found in DLL: " + RuntimeData.DLLFile);
                return;
            }
            Logger.Verbose("Found " + exportedFunctions.Count + " functions");

            Dictionary<string, FunctionSignature> proxyFunctions = new();
            if (!String.IsNullOrEmpty(RuntimeData.GhidraHeadlessPath))
            {
                proxyFunctions = GetFunctionDefinitions(exportedFunctions);
            }

            SolutionGenerator solutionGenerator = new();
            try
            {
                if (!solutionGenerator.Create(RuntimeData.Solution, RuntimeData.DLLFile, exportedFunctions, proxyFunctions))
                {
                    Logger.Error("Could not generate solution");
                    return;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                return;
            }

            Logger.Success("Target solution created at: " + RuntimeData.Solution);
        }

        private Dictionary<string, FunctionSignature> GetFunctionDefinitions(List<FileExport> exportedFunctions)
        {
            Dictionary<string, FunctionSignature> functionDefinitions = new();

            // First we ran Ghidra to get the function definitions.
            string ghidraOutput = GetGhidraOutput();
            if (String.IsNullOrEmpty(ghidraOutput))
            {
                // This will be an empty set.
                return functionDefinitions;
            }

            Logger.Info("Loading function definitions");
            List<FunctionSignature> loadedFunctions = LoadDllFunctionDefinitions(ghidraOutput);

            Logger.Info("Matching exported functions with loaded function definitions");
            functionDefinitions = GetProxyFunctions(exportedFunctions, loadedFunctions, RuntimeData.FunctionsToProxy);

            if (functionDefinitions.Count == 0)
            {
                Logger.Warning("No function signatures found");
                return functionDefinitions; // This will be empty.
            }
            Logger.Verbose("Found " + functionDefinitions.Count + " matching functions");

            return functionDefinitions;
        }

        private string GetGhidraOutput()
        {
            // Create a temporary folder where the Ghidra project will be created into, and where the post-scripts will live.
            string ghidraParentFolder = Path.GetTempPath() + "Spartacus-" + Guid.NewGuid().ToString();
            string ghidraProjectPath = ghidraParentFolder + "-Project";
            string ghidraScriptsPath = ghidraParentFolder + "-Scripts";
            string ghidraScriptFile = Path.Combine(ghidraScriptsPath, "ExportFunctionDefinitionsINI.java");
            string ghidraScriptIniOutput = Path.Combine(ghidraScriptsPath, "ExportedFunctions.ini");

            Logger.Info("Creating Ghidra/Output directories...");
            if (!PrepareRuntimeDirectoriesAndFiles(ghidraProjectPath, ghidraScriptsPath, RuntimeData.Solution, ghidraScriptIniOutput, ghidraScriptFile))
            {
                Logger.Error("Could not prepare runtime directories and files");
                return "";
            }
            Logger.Verbose("Ghidra project path will be: " + ghidraProjectPath);
            Logger.Verbose("Ghidra scripts path will be: " + ghidraScriptsPath);
            Logger.Verbose("Ghidra post-script file will be: " + ghidraScriptFile);
            Logger.Verbose("Output path will be: " + RuntimeData.Solution);

            // Run analyzeHeadless.bat (Ghidra) and get the output ini file. If no file is created, abort.
            Logger.Info("Running Ghidra...");
            ExecuteGhidra(RuntimeData.GhidraHeadlessPath, ghidraProjectPath, ghidraScriptsPath, RuntimeData.DLLFile);

            if (!File.Exists(ghidraScriptIniOutput))
            {
                Logger.Error("Could not find " + ghidraScriptIniOutput);
                Logger.Error("This is because the Ghidra postScript did not execute properly. Run Spartacus with the --debug argument to see Ghidra's output");
                return "";
            }

            // Get the data we need.
            string output = File.ReadAllText(ghidraScriptIniOutput).Replace("\r\n", "\n");

            // And cleanup.
            Logger.Info("Cleaning up...");
            Logger.Info("Deleting Ghidra project path - " + ghidraProjectPath);
            if (!Helper.DeleteTargetDirectory(ghidraProjectPath))
            {
                Logger.Warning("Could not clean up path: " + ghidraProjectPath);
            }

            Logger.Info("Deleting Ghidra scripts path - " + ghidraScriptsPath);
            if (!Helper.DeleteTargetDirectory(ghidraScriptsPath))
            {
                Logger.Warning("Could not clean up path: " + ghidraScriptsPath);
            }

            return output;
        }

        private List<FunctionSignature> LoadDllFunctionDefinitions(string ghidraOutput)
        {
            /*
             * This function will parse this format:
             * 
                [VerFindFileW]
                return=DWORD
                signature=DWORD VerFindFileW(DWORD uFlags, LPCWSTR szFileName, LPCWSTR szWinDir, LPCWSTR szAppDir, LPWSTR szCurDir, PUINT lpuCurDirLen, LPWSTR szDestDir, PUINT lpuDestDirLen)
                parameters[0]=uFlags|DWORD
                parameters[1]=szFileName|LPCWSTR
                parameters[2]=szWinDir|LPCWSTR
                parameters[3]=szAppDir|LPCWSTR
                parameters[4]=szCurDir|LPWSTR
                parameters[5]=lpuCurDirLen|PUINT
                parameters[6]=szDestDir|LPWSTR
                parameters[7]=lpuDestDirLen|PUINT
             */
            List<FunctionSignature> functions = new List<FunctionSignature>();
            FunctionSignature function = new FunctionSignature("");

            string[] lines = ghidraOutput.Split('\n');
            foreach (string line in lines)
            {
                Logger.Debug(line);
                if (line.Trim().Length == 0)
                {
                    continue;
                }

                if (line[0] == '[')
                {
                    if (function.Name.Length > 0)
                    {
                        Logger.Debug("Adding function " + function.Name);
                        Logger.Debug("\tReturn: " + function.Return);
                        Logger.Debug("\tSignature: " + function.Signature);
                        foreach (Parameter p in function.Parameters)
                        {
                            Logger.Debug("\t" + p.Ordinal + ", " + p.Type + ", " + p.Name);
                        }
                        functions.Add(function);
                    }
                    function = new FunctionSignature(line.Trim('[', ']').Trim());
                }
                else
                {
                    string[] data = line.Split(new char[] { '=' }, 2);
                    if (data.Length != 2)
                    {
                        continue;
                    }

                    if (data[0] == "return")
                    {
                        function.Return = data[1].Trim().ToUpper();
                    }
                    else if (data[0] == "signature")
                    {
                        function.Signature = data[1].Trim();
                    }
                    else if (data[0].StartsWith("parameters["))
                    {
                        string[] info = data[1].Split(new char[] { '|' }, 2);
                        if (info.Length != 2)
                        {
                            continue;
                        }
                        int Ordinal = Int32.Parse(data[0].Replace("parameters", "").Replace("[", "").Replace("]", ""));
                        function.CreateParameter(Ordinal, info[0], info[1]);
                    }
                }
            }

            return functions;
        }

        private void ExecuteGhidra(string headlessAnalyserPath, string projectPath, string scriptPath, string dllPath)
        {
            Process p = new Process();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.FileName = headlessAnalyserPath;
            p.StartInfo.Arguments = $"\"{projectPath}\" SpartacusProject -import \"{dllPath}\" -scriptPath \"{scriptPath}\" -postScript ExportFunctionDefinitionsINI.java -deleteProject";

            Logger.Verbose("Executing " + p.StartInfo.FileName);
            Logger.Verbose("Command Line: " + p.StartInfo.Arguments);

            p.Start();

            Logger.Verbose("Waiting for Ghidra to finish...");
            string standardOutput = p.StandardOutput.ReadToEnd();
            string errorOutput = p.StandardError.ReadToEnd();
            p.WaitForExit();
            Logger.Info("Ghidra has finished");

            Logger.Debug("Ghidra output");
            Logger.Debug(standardOutput);
            Logger.Debug("Ghidra errors");
            Logger.Debug(errorOutput);
        }

        private bool PrepareRuntimeDirectoriesAndFiles(string ghidraProjectPath, string ghidraScriptsPath, string outputDirectory, string ghidraScriptIniOutput, string ghidraScriptFile)
        {
            // First create all required directories.
            if (!Helper.CreateTargetDirectory(ghidraProjectPath))
            {
                Logger.Error("Could not create path: " + ghidraProjectPath);
                return false;
            }
            else if (!Helper.CreateTargetDirectory(ghidraScriptsPath))
            {
                Logger.Error("Could not create path: " + ghidraScriptsPath);
                return false;
            }
            else if (!Helper.CreateTargetDirectory(outputDirectory))
            {
                Logger.Error("Could not create output path: " + outputDirectory);
                return false;
            }

            // Get the Ghidra post-script that will export all the function definitions.
            Logger.Info("Creating Ghidra postScript...");
            string ghidraScriptContents = Helper.GetResource("ExportFunctionDefinitionsINI.java")
                .Replace("%EXPORT_TO%", ghidraScriptIniOutput.Replace("\\", "\\\\"));

            try
            {
                File.WriteAllText(ghidraScriptFile, ghidraScriptContents);
            }
            catch (Exception ex)
            {
                Logger.Warning("Could not create " + ghidraScriptFile);
                Logger.Error(ex.Message);
                return false;
            }

            return true;
        }        

        private Dictionary<string, FunctionSignature> GetProxyFunctions(List<FileExport> exportedFunctions, List<FunctionSignature> loadedFunctions, List<string> onlyProxyFunctions)
        {
            Dictionary<string, FunctionSignature> proxyFunctions = new Dictionary<string, FunctionSignature>();
            foreach (FileExport exportedFunction in exportedFunctions)
            {
                if (onlyProxyFunctions.Count > 0 && !onlyProxyFunctions.Contains(exportedFunction.Name.ToLower()))
                {
                    Logger.Verbose("Skipping function because it's not in the --only list: " + exportedFunction.Name);
                    continue;
                }

                foreach (FunctionSignature function in loadedFunctions)
                {
                    // We only need functions that don't have any "undefined" parameters.
                    if (exportedFunction.Name != function.Name)
                    {
                        continue;
                    }

                    if (function.Return.ToLower().StartsWith("undefined"))
                    {
                        continue;
                    }

                    bool hasUndefined = false;
                    foreach (Parameter p in function.Parameters)
                    {
                        if (p.Type.ToLower().StartsWith("undefined"))
                        {
                            hasUndefined = true;
                            break;
                        }
                    }

                    if (hasUndefined)
                    {
                        continue;
                    }

                    proxyFunctions.Add(function.Name, function);
                    break;
                }
            }
            return proxyFunctions;
        }

        public override void SanitiseAndValidateRuntimeData()
        {
            // Check for the DLL file to proxy.
            if (String.IsNullOrEmpty(RuntimeData.DLLFile))
            {
                throw new Exception("--dll is missing");
            }
            else if (!File.Exists(RuntimeData.DLLFile))
            {
                throw new Exception("--dll file does not exist: " + RuntimeData.DLLFile);
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
                if (!File.Exists(RuntimeData.GhidraHeadlessPath)) {
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
        }
    }
}
