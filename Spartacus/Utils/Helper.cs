using Spartacus.Spartacus;
using Spartacus.Spartacus.CommandLine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using static Spartacus.Spartacus.PEFileExports;

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

        public void ExportDLLExports(Dictionary<string, string> filesToProxy, string outputDirectory)
        {
            Logger.Info("Extracting DLL export functions...");

            if (filesToProxy.Count() == 0)
            {
                return;
            }

            PEFileExports ExportLoader = new PEFileExports();

            foreach (KeyValuePair<string, string> file in filesToProxy)
            {
                Logger.Info("Processing " + file.Key, false, true);
                string saveAs = Path.Combine(outputDirectory, $"{file.Key}.cpp");

                if (String.IsNullOrEmpty(file.Value))
                {
                    File.Create(saveAs + "-file-not-found").Dispose();
                    Logger.Warning(" - No DLL Found", true, false);
                    continue;
                }

                string actualLocation = LookForFileIfNeeded(file.Value);
                if (!File.Exists(actualLocation))
                {
                    File.Create(saveAs + "-file-not-found").Dispose();
                    Logger.Warning(" - No DLL Found", true, false);
                    continue;
                }

                string actualPathNoExtension = Path.Combine(Path.GetDirectoryName(actualLocation), Path.GetFileNameWithoutExtension(actualLocation));

                List<FileExport> exports = new List<FileExport>();
                try
                {
                    exports = ExportLoader.Extract(actualLocation);
                }
                catch (Exception e)
                {
                    // Nothing.
                }

                if (exports.Count == 0)
                {
                    File.Create(saveAs + "-no-exports-found").Dispose();
                    Logger.Warning(" - No export functions found", true, false);
                    continue;
                }

                List<string> pragma = new List<string>();
                string pragmaTemplate = "#pragma comment(linker,\"/export:{0}={1}.{2},@{3}\")";
                int steps = exports.Count() / 10;
                if (steps == 0)
                {
                    steps = 1;
                }
                int counter = 0;
                foreach (FileExport f in exports)
                {
                    if (++counter % steps == 0)
                    {
                        Logger.Info(".", false, false);
                    }
                    pragma.Add(String.Format(pragmaTemplate, f.Name, actualPathNoExtension.Replace("\\", "\\\\"), f.Name, f.Ordinal));
                }

                string fileContents = RuntimeData.TemplateProxyDLL.Replace("%_PRAGMA_COMMENTS_%", String.Join("\r\n", pragma.ToArray()));
                File.WriteAllText(saveAs, fileContents);

                Logger.Success("OK", true, false);
            }
        }

        private string LookForFileIfNeeded(string filePath)
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
    }
}
