using Spartacus.Modes.PROXY;
using Spartacus.Properties;
using Spartacus.Spartacus;
using Spartacus.Spartacus.CommandLine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static Spartacus.Modes.PROXY.PrototypeDatabaseGeneration;
using static Spartacus.Utils.PEFileExports;

namespace Spartacus.Utils
{
    class Helper
    {
        [DllImport("shell32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        static extern int SHGetSpecialFolderPath(IntPtr hwndOwner, IntPtr lpszPath, int nFolder, int fCreate);

        // This is what we consider "OS Paths".
        private const int CSIDL_WINDOWS = 0x0024;

        private const int CSIDL_SYSTEM = 0x0025;
        private const int CSIDL_SYSTEMX86 = 0x0029;

        private const int CSIDL_PROGRAM_FILES = 0x0026;
        private const int CSIDL_PROGRAM_FILESX86 = 0x002a;

        public CurrentUserACL UserACL = new();

        private string GetSpecialFolder(int folder)
        {
            IntPtr path = Marshal.AllocHGlobal(260 * 2); // Unicode.
            SHGetSpecialFolderPath(IntPtr.Zero, path, folder, 0);
            string result = Marshal.PtrToStringUni(path);
            Marshal.FreeHGlobal(path);
            return result;
        }

        public List<string> GetOSPaths()
        {
            return new List<string>
            {
                GetSpecialFolder(CSIDL_WINDOWS).ToLower(),
                GetSpecialFolder(CSIDL_SYSTEM).ToLower(),
                GetSpecialFolder(CSIDL_SYSTEMX86).ToLower(),
                GetSpecialFolder(CSIDL_PROGRAM_FILES).ToLower(),
                GetSpecialFolder(CSIDL_PROGRAM_FILESX86).ToLower()
            };
        }

        public string LookForFileIfNeeded(string filePath)
        {
            // When we get a path it may be either x32 or a x64. As Spartacus is x64 we can search in the x32 locations if needed.
            if (File.Exists(filePath))
            {
                return filePath;
            }

            // There should really be a case-insensitive replace.
            if (filePath.StartsWith(Environment.GetFolderPath(Environment.SpecialFolder.System), StringComparison.OrdinalIgnoreCase))
            {
                return Environment.GetFolderPath(Environment.SpecialFolder.SystemX86) + filePath.Remove(0, Environment.GetFolderPath(Environment.SpecialFolder.System).Length);
            }
            else if (filePath.StartsWith(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), StringComparison.OrdinalIgnoreCase))
            {
                return Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) + filePath.Remove(0, Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles).Length);
            }

            // Otherwise return the original value.
            return filePath;
        }

        public bool CreateTargetDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                return true;
            }

            try
            {
                Directory.CreateDirectory(path);
            }
            catch (Exception ex)
            {
                return false;
            }
            return Directory.Exists(path);
        }

        public bool DeleteTargetDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                return true;
            }

            try
            {
                Directory.Delete(path, true);
            }
            catch (Exception ex)
            {
                return false;
            }

            return !Directory.Exists(path);
        }

        public string GetResource(string name, bool isInternal)
        {
            string data;

            if (RuntimeData.UseExternalResources && !isInternal)
            {
                string fullPath = Path.GetFullPath(@$"Assets\{name}");
                if (!File.Exists(fullPath))
                {
                    throw new Exception("Could not load external resource: " + fullPath);
                }

                data = File.ReadAllText(fullPath);
            }
            else
            {
                data = Resources.ResourceManager.GetString(name);
            }

            if (String.IsNullOrEmpty(data))
            {
                throw new Exception("Loaded resource is empty: " + name);
            }
            return data;
        }

        public string GetResource(string name)
        {
            return GetResource(name, false);
        }

        public List<FileExport> GetExportFunctions(string DLL)
        {
            List<FileExport> exports = new();
            PEFileExports ExportLoader = new();

            try
            {
                exports = ExportLoader.Extract(DLL);
            }
            catch (Exception ex)
            {
                Logger.Error("Could not get exports for file: " + DLL);
                Logger.Error(ex.Message);
            }

            return exports;
        }

        public string GetHelp()
        {
            string helpText = RuntimeData.Mode switch
            {
                RuntimeData.SpartacusMode.DLL => @"help\dll.txt",
                RuntimeData.SpartacusMode.PROXY => @"help\proxy.txt",
                RuntimeData.SpartacusMode.COM => @"help\com.txt",
                RuntimeData.SpartacusMode.DETECT => @"help\detect.txt",
                RuntimeData.SpartacusMode.SIGN => @"help\sign.txt",
                _ => @"help\main.txt"
            };

            return GetResource(helpText, true);
        }

        public string ExtractGUIDFromString(string text)
        {
            string pattern = @"([a-f0-9]{8}[-][a-f0-9]{4}[-][a-f0-9]{4}[-][a-f0-9]{4}[-][a-f0-9]{12})";
            MatchCollection matches = Regex.Matches(text.ToLower(), pattern);
            return matches.Count > 0 ? "{" + matches[0].ToString().ToUpper() + "}" : "";
        }

        public List<FunctionPrototype> LoadPrototypes(string inputFile)
        {
            if (String.IsNullOrEmpty(inputFile) || !File.Exists(inputFile))
            {
                return new();
            }

            Logger.Info("Loading external function prototypes from " + inputFile);
            PrototypeDatabaseGeneration generator = new();
            return generator.LoadPrototypesFromCSV(inputFile);
        }
    }
}
