using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spartacus.Modes.SIGN.MsSign
{
    public interface ISigningTool
    {
        /// <summary>
        /// Gets the name of the format the signing tool offers to sign.
        /// </summary>
        string FormatName { get; }

        /// <summary>
        /// Gets the list of hash algorithms supported by this signing tool.
        /// </summary>
        IReadOnlyList<string> SupportedHashAlgorithms { get; }

        /// <summary>
        /// Performs the signing of the given file through the request.
        /// Might throw any exceptions describing the error during signing.
        /// </summary>
        /// <param name="signFileRequest">The request describing what to sign.</param>
        /// <param name="cancellationToken">A token to support cancellation.</param>
        /// <returns>The result of the signing operation.</returns>
        SignFileResponse SignFile(SignFileRequest signFileRequest);

        /// <summary>
        /// Checks whether the given file is signed.
        /// </summary>
        /// <param name="inputFileName">The path to the file on disk.</param>
        /// <param name="cancellationToken">A token to support cancellation.</param>
        /// <returns>true if the file is considered signed, otherwise false.</returns>
        /// <remarks>
        /// Some tools might only do a very basic check and not a full validation on whether
        /// all aspects of the signing are in place and valid.
        /// </remarks>
        bool IsFileSigned(string inputFileName);
    }
}
