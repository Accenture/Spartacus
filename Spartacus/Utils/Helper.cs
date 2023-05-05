using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

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
    }
}
