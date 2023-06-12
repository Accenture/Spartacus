using Spartacus.Modes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spartacus.Spartacus.CommandLine
{
    class RuntimeData
    {
        public enum SpartacusMode
        {
            NONE = 0,
            DLL = 1,
        };

        public static SpartacusMode Mode = SpartacusMode.NONE;

        public static bool Verbose = false;

        public static bool Debug = false;

        public static ModeBase ModeObject = null;

        public static string PMLFile = "";

        public static string PMCFile = "";

        public static string CSVFile = "";

        public static string ProcMonExecutable = "";

        public static bool IsExistingLog = false;

        public static bool InjectBackingFileIntoConfig = false;

        public static bool All = false;

        public static string ExportsDirectory = "";

        public static string TemplateProxyDLL = "";








        // OLD.
        public static string ProcMonConfigFile = "";

        public static string ProcMonLogFile = "";

        public static string CsvOutputFile = "";

        

        public static string ExportsOutputDirectory = "";

        public static string ProxyDllTemplate = "";

        public static bool ProcessExistingLog = false;

        public static List<string> TrackExecutables = new List<string>();

        

        

        public static bool IncludeAllDLLs = false;

        public static bool DetectProxyingDLLs = false;

        public static bool GenerateProxy = false;

        public static string GhidraHeadlessPath = "";

        public static string DLL = "";

        public static string OutputDirectory = "";

        public static List<string> OnlyProxy = new List<string>();
    }
}
