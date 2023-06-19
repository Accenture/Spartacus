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
            DETECT = 2,
            PROXY = 3,
            COM = 4,
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

        public static string Solution = "";

        public static string DLLFile = "";

        public static List<string> FunctionsToProxy = new();

        public static string GhidraHeadlessPath = "";

        public static bool Overwrite = false;

        public static bool UseExternalResources = false;

        public static bool isACL = false;

        public static bool isHelp = false;
    }
}
