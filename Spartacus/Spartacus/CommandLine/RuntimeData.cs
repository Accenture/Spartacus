using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spartacus.Spartacus.CommandLine
{
    class RuntimeData
    {
        public static string ProcMonConfigFile = "";

        public static string ProcMonLogFile = "";

        public static string CsvOutputFile = "";

        public static string ProcMonExecutable = "";

        public static string ExportsOutputDirectory = "";

        public static string ProxyDllTemplate = "";

        public static bool ProcessExistingLog = false;

        public static List<string> TrackExecutables = new List<string>();

        public static bool Verbose = false;

        public static bool Debug = false;

        public static bool InjectBackingFileIntoConfig = false;

        public static bool IncludeAllDLLs = false;

        public static bool DetectProxyingDLLs = false;

        public static bool GenerateProxy = false;

        public static string GhidraHeadlessPath = "";

        public static string DLL = "";

        public static string OutputDirectory = "";

        public static List<string> OnlyProxy = new List<string>();
    }
}
