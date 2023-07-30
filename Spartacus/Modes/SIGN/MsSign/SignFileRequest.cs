using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Spartacus.Modes.SIGN.MsSign
{
    public class SignFileRequest
    {
        /// <summary>
        /// Gets or sets the absolute path to the file being signed.
        /// </summary>
        public string InputFilePath { get; set; }

        /// <summary>
        /// Gets or sets the certificate used during the signing operation.
        /// Typically embedded into the signed file (without private keys).
        /// </summary>
        public X509Certificate2 Certificate { get; set; }

        /// <summary>
        /// Gets or sets the private key used for performing the signing operations.
        /// This key must match the <see cref="Certificate"/> to avoid corrupt signatures.
        /// </summary>
        public AsymmetricAlgorithm PrivateKey { get; set; }

        /// <summary>
        /// Gets or sets the original name of the file being signed. <see cref="InputFilePath"/>
        /// might point to a temporarily name while <see cref="OriginalFileName"/> is the name of
        /// the file as provided by the client. Might be used to generate auxiliary files.
        /// </summary>
        public string OriginalFileName { get; set; }

        /// <summary>
        /// Gets or sets the timestamping server which should be used for timestamping the signatures.
        /// </summary>
        public string TimestampServer { get; set; }

        /// <summary>
        /// Gets or sets the name of the hash algorithm to be used for the signatures.
        /// </summary>
        public string HashAlgorithm { get; set; }

        /// <summary>
        /// Gets or sets whether any existing signatures should be overwritten.
        /// If this is not set, and a file is already signed, the signing operation will fail.
        /// </summary>
        public bool OverwriteSignature { get; set; }
    }
}
