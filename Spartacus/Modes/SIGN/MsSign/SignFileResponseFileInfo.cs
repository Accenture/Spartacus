using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spartacus.Modes.SIGN.MsSign
{
    public class SignFileResponseFileInfo
    {
        /// <summary>
        /// The name of the output file as it should be named on the client side.
        /// </summary>
        public string FileName { get; }

        /// <summary>
        /// The full path to the disk holding the output file which should be sent to the client.
        /// </summary>
        public string OutputFilePath { get; }

        public SignFileResponseFileInfo(string fileName, string outputFilePath)
        {
            FileName = fileName;
            OutputFilePath = outputFilePath;
        }
    }
}
