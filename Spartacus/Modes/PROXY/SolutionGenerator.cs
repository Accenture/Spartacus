using Spartacus.Spartacus.Models;
using Spartacus.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading.Tasks;
using static Spartacus.Spartacus.PEFileExports;

namespace Spartacus.Modes.PROXY
{
    class SolutionGenerator
    {
        protected Helper Helper = new();

        public bool Create(string solutionDirectory, string dllPath, List<FileExport> exportedFunctions, Dictionary<string, FunctionSignature> proxyFunctions)
        {
            Dictionary<string, string> variables;
            string dllName = Path.GetFileName(dllPath);
            string projectName = Path.GetFileNameWithoutExtension(dllPath);

            if (!File.Exists(dllPath))
            {
                throw new Exception("DLL does not exist: " + dllPath);
            }
            else if (exportedFunctions.Count == 0)
            {
                throw new Exception("No exported functions passed");
            }

            if (!Helper.CreateTargetDirectory(solutionDirectory))
            {
                throw new Exception("Could not create solution target directory: " + solutionDirectory);
            }

            // Generate .sln file
            variables = new Dictionary<string, string>()
            {
                { "%_PROJECTNAME_%", projectName }
            };
            if (!GenerateProjectFile(solutionDirectory, @"solution\proxy.sln", $"{projectName}.sln", variables))
            {
                throw new Exception("Could not create solution file");
            }

            // Generate vcxproj file.
            variables = new Dictionary<string, string>()
            {
                { "%_PROJECTNAME_%", projectName },     // This is the name of the project that appears in the project explorer.
                { "%_SOURCEDLL_%", dllPath },           // This is the path to the source DLL that is used for timestomping the output file post-build.
            };
            if (!GenerateProjectFile(solutionDirectory, @"solution\proxy.vcxproj", $"{projectName}.vcxproj", variables))
            {
                throw new Exception("Could not create project file");
            }

            // Generate resource.h file.
            variables = new Dictionary<string, string>();
            if (!GenerateProjectFile(solutionDirectory, @"solution\resource.h", "resource.h", variables))
            {
                throw new Exception("Could not create resource.h file");
            }

            // Generate the resource file *.rc.
            if (!GenerateResourceFile(solutionDirectory, @"solution\proxy.rc", $"{projectName}.rc", dllPath))
            {
                throw new Exception("Could not create resource file");
            }

            // Generate *.def file.
            if (!GenerateDefinitionsFile(solutionDirectory, $"{projectName}.def", dllName, proxyFunctions))
            {
                throw new Exception("Could not create definitions file");
            }

            // Generate the main file.
            if (!GenerateDllMainFile(solutionDirectory, @"solution\dllmain.cpp", "dllmain.cpp", dllPath, proxyFunctions, exportedFunctions, projectName))
            {
                throw new Exception("Could not create dllmain.cpp file");
            }

            return true;
        }

        private bool GenerateProjectFile(string solutionDirectory, string resourceName, string saveAsFilename, Dictionary<string, string> variables)
        {
            return GenerateProjectFile(solutionDirectory, resourceName, saveAsFilename, variables, false);
        }

        private bool GenerateProjectFile(string solutionDirectory, string resourceName, string saveAsFilename, Dictionary<string, string> variables, bool writeAsUnicode)
        {
            Logger.Verbose($"Generating {saveAsFilename}");
            string contents = Helper.GetResource(resourceName);

            foreach (KeyValuePair<string, string> variable in variables)
            {
                contents = contents.Replace(variable.Key, variable.Value);
            }

            Logger.Debug($"Saving {saveAsFilename}");
            string saveAs = Path.Combine(solutionDirectory, saveAsFilename);

            if (writeAsUnicode)
            {
                using (var f = File.Create(saveAs))
                {
                    using (StreamWriter sw = new StreamWriter(f, Encoding.Unicode))
                    {
                        sw.Write(contents);
                    }
                }
            }
            else
            {
                File.WriteAllText(saveAs, contents);
            }

            return File.Exists(saveAs);
        }

        private bool GenerateResourceFile(string solutionDirectory, string resourceName, string saveAsFilename, string dllPath)
        {
            Dictionary<string, string> variables;

            try
            {
                FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(dllPath);
                string fileVersion = versionInfo.FileVersion.Split(' ')[0];

                // For some reason FileVersion doesn't always match what is displayed when you view the file's properties.
                string[] fileVersionParts = fileVersion.Split('.');

                if (fileVersionParts.Length != 4)
                {
                    fileVersionParts = new string[4];
                    fileVersionParts[0] = versionInfo.ProductMajorPart.ToString();
                    fileVersionParts[1] = versionInfo.ProductMinorPart.ToString();
                    fileVersionParts[2] = versionInfo.ProductBuildPart.ToString();
                    fileVersionParts[3] = versionInfo.ProductPrivatePart.ToString();
                }

                variables = new()
                {
                    { "%_COMPANYNAME_%", versionInfo.CompanyName },
                    { "%_FILEDESCRIPTION_%", versionInfo.FileDescription },
                    { "%_INTERNALNAME_%", versionInfo.InternalName },
                    { "%_LEGALCOPYRIGHT_%", versionInfo.LegalCopyright },
                    { "%_ORIGINALNAME_%", versionInfo.OriginalFilename },
                    { "%_PRODUCTNAME_%", versionInfo.ProductName },
                    { "%_PRODUCTVERSION_%", versionInfo.ProductVersion },
                    { "%_PRODUCTVERSION_MAJOR_%", versionInfo.ProductMajorPart.ToString() },
                    { "%_PRODUCTVERSION_MINOR_%", versionInfo.ProductMinorPart.ToString() },
                    { "%_PRODUCTVERSION_BUILD_%", versionInfo.ProductBuildPart.ToString() },
                    { "%_PRODUCTVERSION_REVISION_%", versionInfo.ProductPrivatePart.ToString() },
                    { "%_FILEVERSION_%", fileVersion },
                    { "%_FILEVERSION_MAJOR_%", fileVersionParts[0] },
                    { "%_FILEVERSION_MINOR_%", fileVersionParts[1] },
                    { "%_FILEVERSION_BUILD_%", fileVersionParts[2] },
                    { "%_FILEVERSION_REVISION_%", fileVersionParts[3] }
                };
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                return false;
            }

            return GenerateProjectFile(solutionDirectory, resourceName, saveAsFilename, variables, true);
        }

        private bool GenerateDefinitionsFile(string solutionDirectory, string saveAsFilename, string dllName, Dictionary<string, FunctionSignature> proxyFunctions)
        {
            Logger.Verbose($"Generating {saveAsFilename}");

            List<string> lines = new()
            {
                $"LIBRARY {dllName}",
            };

            if (proxyFunctions.Count > 0)
            {
                lines.Add("EXPORTS");
            }

            foreach (KeyValuePair<string, FunctionSignature> item in proxyFunctions)
            {
                lines.Add("\t" + item.Value.Name + "=" + item.Value.GetProxyName());
            }
            string contents = String.Join("\r\n", lines.ToArray());

            Logger.Debug($"Saving {saveAsFilename}");
            string saveAs = Path.Combine(solutionDirectory, saveAsFilename);

            File.WriteAllText(saveAs, contents);
            
            return File.Exists(saveAs);
        }

        private bool GenerateDllMainFile(string solutionDirectory, string resourceName, string saveAsFilename, string dllPath, Dictionary<string, FunctionSignature> proxyFunctions, List<FileExport> exportedFunctions, string projectName)
        {
            string contents = Helper.GetResource(resourceName);

            // First generate the pragma comments.
            List<string> pragma = new();
            string pragmaTemplate = "#pragma comment(linker,\"/export:{0}={1}.{2},@{3}\")";
            string actualPathNoExtension = Path.Combine(Path.GetDirectoryName(dllPath), Path.GetFileNameWithoutExtension(dllPath));
            foreach (FileExport f in exportedFunctions)
            {
                string line = String.Format(pragmaTemplate, f.Name, actualPathNoExtension.Replace("\\", "\\\\"), f.Name, f.Ordinal);
                if (proxyFunctions.ContainsKey(f.Name))
                {
                    // Comment out if it's a proxied function.
                    line = $"// {line}";
                }
                pragma.Add(line);
            }

            List<string> typeDef = new();
            List<string> functions = new();
            foreach (KeyValuePair<string, FunctionSignature> item in proxyFunctions)
            {
                typeDef.Add(item.Value.GetTypedefDeclaration() + ";");
                functions.Add(item.Value.GetProxyFunctionCode(true));
            }

            contents = contents
                .Replace("%_PRAGMA_COMMENTS_%", String.Join("\r\n", pragma.ToArray()))
                .Replace("%_TYPEDEF_%", String.Join("\r\n", typeDef.ToArray()))
                .Replace("%_FUNCTIONS_%", String.Join("\r\n", functions.ToArray()))
                .Replace("%_PROJECTNAME_%", projectName)
                .Replace("%_REAL_DLL_%", dllPath.Replace("\\", "\\\\"));


            Logger.Debug($"Saving {saveAsFilename}");
            string saveAs = Path.Combine(solutionDirectory, saveAsFilename);

            File.WriteAllText(saveAs, contents);

            return File.Exists(saveAs);
        }
    }
}
