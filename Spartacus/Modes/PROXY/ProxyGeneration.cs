using Spartacus.Spartacus.CommandLine;
using Spartacus.Spartacus.Models;
using Spartacus.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Spartacus.Modes.PROXY.PrototypeDatabaseGeneration;
using static Spartacus.Spartacus.Models.FunctionSignature;
using static Spartacus.Spartacus.PEFileExports;

namespace Spartacus.Modes.PROXY
{
    class ProxyGeneration
    {
        protected Helper Helper = new();

        protected List<FunctionPrototype> ExistingFunctionPrototypes = new();

        public void Run()
        {
            ExistingFunctionPrototypes = Helper.LoadPrototypes(RuntimeData.PrototypesFile);

            foreach (string dllFile in RuntimeData.BatchDLLFiles)
            {
                string solutionPath = RuntimeData.BatchDLLFiles.Count == 1 ? RuntimeData.Solution : Path.Combine(RuntimeData.Solution, Path.GetFileNameWithoutExtension(dllFile));
                if (!ProcessSingleDLL(dllFile, solutionPath))
                {
                    Logger.Error("Could not generate proxy DLL for: " + dllFile);
                }
            }
        }

        public bool ProcessSingleDLL(string dllFile, string solutionPath)
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

            Logger.Verbose("Extracting DLL export functions from: " + dllFile);
            List<FileExport> exportedFunctions = Helper.GetExportFunctions(dllFile);
            if (exportedFunctions.Count == 0)
            {
                Logger.Warning("No export functions found in DLL: " + dllFile);
                return false;
            }
            Logger.Verbose("Found " + exportedFunctions.Count + " functions");

            foreach (var item in exportedFunctions)
            {
                Logger.Debug(item.Name);
            }

            Dictionary<string, FunctionSignature> proxyFunctions = new();
            if (!String.IsNullOrEmpty(RuntimeData.GhidraHeadlessPath))
            {
                proxyFunctions = GetFunctionDefinitions(exportedFunctions, dllFile, solutionPath);
            }

            if (ExistingFunctionPrototypes.Count > 0)
            {
                proxyFunctions = GetFunctionDefinitionsFromPrototypes(exportedFunctions, proxyFunctions);
            }

            proxyFunctions = FilterProxyFunctions(proxyFunctions, RuntimeData.FunctionsToProxy);

            SolutionGenerator solutionGenerator = new();
            try
            {
                if (!solutionGenerator.Create(solutionPath, dllFile, exportedFunctions, proxyFunctions))
                {
                    Logger.Error("Could not generate solution");
                    return false;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                return false;
            }

            Logger.Success("Target solution created at: " + solutionPath);

            return true;
        }

        private Dictionary<string, FunctionSignature> FilterProxyFunctions(Dictionary<string, FunctionSignature> proxyFunctions, List<string> onlyProxy)
        {
            if (onlyProxy.Count == 0)
            {
                return proxyFunctions;
            }

            Logger.Info("Filtering out functions selected for proxying...");
            Dictionary<string, FunctionSignature> finalFunctions = new();
            foreach (KeyValuePair<string, FunctionSignature> function in proxyFunctions)
            {
                if (onlyProxy.Contains(function.Key.ToLower()))
                {
                    finalFunctions[function.Key] = function.Value;
                }
            }

            return finalFunctions;
        }

        private Dictionary<string, FunctionSignature> GetFunctionDefinitions(List<FileExport> exportedFunctions, string dllFile, string solutionPath)
        {
            Dictionary<string, FunctionSignature> functionDefinitions = new();

            // First we ran Ghidra to get the function definitions.
            string ghidraOutput = GetGhidraOutput(dllFile, solutionPath);
            if (String.IsNullOrEmpty(ghidraOutput))
            {
                // This will be an empty set.
                return functionDefinitions;
            }

            Logger.Info("Loading function definitions");
            List<FunctionSignature> loadedFunctions = LoadDllFunctionDefinitions(ghidraOutput);

            Logger.Verbose("Matching exported functions with loaded function definitions");
            functionDefinitions = GetProxyFunctions(exportedFunctions, loadedFunctions);

            if (functionDefinitions.Count == 0)
            {
                Logger.Warning("No function signatures found");
                return functionDefinitions; // This will be empty.
            }
            Logger.Verbose("Found " + functionDefinitions.Count + " matching functions");

            return functionDefinitions;
        }

        private Dictionary<string, FunctionSignature> GetFunctionDefinitionsFromPrototypes(List<FileExport> exportedFunctions, Dictionary<string, FunctionSignature> proxyFunctions)
        {
            foreach (FileExport exportedFunction in exportedFunctions)
            {
                if (proxyFunctions.ContainsKey(exportedFunction.Name))
                {
                    // Function has already been added from the Ghidra output.
                    continue;
                }

                List<FunctionPrototype> functions = ExistingFunctionPrototypes.Where(x => x.name == exportedFunction.Name).ToList();
                if (functions.Count == 0)
                {
                    // Function was not found in the prototype database.
                    continue;
                }

                FunctionPrototype function = functions.First();
                FunctionSignature signature = new(function.name)
                {
                    Return = function.returnType
                };

                foreach (FunctionArgument argument in function.arguments)
                {
                    signature.AddParameter(new Parameter() { Name = argument.name, Type = argument.type });
                }

                proxyFunctions.Add(function.name, signature);
            }

            return proxyFunctions;
        }

        private string GetGhidraOutput(string dllFile, string solutionPath)
        {
            // Create a temporary folder where the Ghidra project will be created into, and where the post-scripts will live.
            string ghidraParentFolder = Path.GetTempPath() + "Spartacus-" + Guid.NewGuid().ToString();
            string ghidraProjectPath = ghidraParentFolder + "-Project";
            string ghidraScriptsPath = ghidraParentFolder + "-Scripts";
            string ghidraScriptFile = Path.Combine(ghidraScriptsPath, "ExportFunctionDefinitionsINI.java");
            string ghidraScriptIniOutput = Path.Combine(ghidraScriptsPath, "ExportedFunctions.ini");

            Logger.Info("Creating Ghidra/Output directories...");
            if (!PrepareRuntimeDirectoriesAndFiles(ghidraProjectPath, ghidraScriptsPath, solutionPath, ghidraScriptIniOutput, ghidraScriptFile))
            {
                Logger.Error("Could not prepare runtime directories and files");
                return "";
            }
            Logger.Verbose("Ghidra project path will be: " + ghidraProjectPath);
            Logger.Verbose("Ghidra scripts path will be: " + ghidraScriptsPath);
            Logger.Verbose("Ghidra post-script file will be: " + ghidraScriptFile);
            Logger.Verbose("Output path will be: " + solutionPath);

            // Run analyzeHeadless.bat (Ghidra) and get the output ini file. If no file is created, abort.
            Logger.Info("Running Ghidra...");
            ExecuteGhidra(RuntimeData.GhidraHeadlessPath, ghidraProjectPath, ghidraScriptsPath, dllFile);

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
            Logger.Verbose("Deleting Ghidra project path - " + ghidraProjectPath);
            if (!Helper.DeleteTargetDirectory(ghidraProjectPath))
            {
                Logger.Warning("Could not clean up path: " + ghidraProjectPath);
            }

            Logger.Verbose("Deleting Ghidra scripts path - " + ghidraScriptsPath);
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

            Logger.Debug("Executing " + p.StartInfo.FileName);
            Logger.Debug("Command Line: " + p.StartInfo.Arguments);

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
            Logger.Verbose("Creating Ghidra postScript...");
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

        private Dictionary<string, FunctionSignature> GetProxyFunctions(List<FileExport> exportedFunctions, List<FunctionSignature> loadedFunctions)
        {
            Dictionary<string, FunctionSignature> proxyFunctions = new Dictionary<string, FunctionSignature>();
            foreach (FileExport exportedFunction in exportedFunctions)
            {
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
    }
}
