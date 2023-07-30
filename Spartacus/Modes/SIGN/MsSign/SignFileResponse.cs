using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spartacus.Modes.SIGN.MsSign
{
    public enum SignFileResponseStatus
    {
        /// <summary>
        /// File was successfully signed
        /// </summary>
        FileSigned,

        /// <summary>
        /// Files was successfully signed, an existing signature was removed
        /// </summary>
        FileResigned,

        /// <summary>
        /// The file was already signed and therefore signing was skipped. 
        /// </summary>
        FileAlreadySigned,

        /// <summary>
        /// The file was not signed because the given file format cannot be signed or is not supported.
        /// </summary>
        FileNotSignedUnsupportedFormat,

        /// <summary>
        /// The file was not signed because an unexpected error happened.
        /// </summary>
        FileNotSignedError,

        /// <summary>
        /// The file was not signed because the singing request was noth authorized.
        /// </summary>
        FileNotSignedUnauthorized
    }

    public class SignFileResponse
    {
        /// <summary>
        /// The result status of the signing
        /// </summary>
        public SignFileResponseStatus Status { get; set; }

        /// <summary>
        /// The detailed error message in case <see cref="Status"/> is set to <see cref="SignFileResponseStatus.FileNotSignedError"/>
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// The result files consisting typically of the signed file.
        /// In some scenarios additional files might be provided (e.g. Android v4 idsig)
        /// </summary>
        public IList<SignFileResponseFileInfo> ResultFiles { get; set; }
    }
}
