using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Spartacus.Modes.SIGN.MsSign
{
    /*
     * This class was taken and customised from https://github.com/Danielku15/SigningServer, under MIT License.
     */
    public class PortableExecutableSigningTool : ISigningTool
    {
        private static readonly Dictionary<string, (uint algId, string algOid, HashAlgorithmName algName)>
        PeSupportedHashAlgorithms =
            new(StringComparer
                .OrdinalIgnoreCase)
            {
                ["SHA1"] = (Win32SigningAPI.CALG_SHA1, Win32SigningAPI.OID_OIWSEC_SHA1, HashAlgorithmName.SHA1),
                ["MD5"] = (Win32SigningAPI.CALG_MD5, Win32SigningAPI.OID_RSA_MD5, HashAlgorithmName.MD5),
                ["SHA256"] = (Win32SigningAPI.CALG_SHA_256, Win32SigningAPI.OID_OIWSEC_SHA256, HashAlgorithmName.SHA256),
                ["SHA384"] = (Win32SigningAPI.CALG_SHA_384, Win32SigningAPI.OID_OIWSEC_SHA384, HashAlgorithmName.SHA384),
                ["SHA512"] = (Win32SigningAPI.CALG_SHA_512, Win32SigningAPI.OID_OIWSEC_SHA512, HashAlgorithmName.SHA512)
            };

        public virtual string FormatName => "Windows Portable Executables (PE)";

        public virtual IReadOnlyList<string> SupportedHashAlgorithms => PeSupportedHashAlgorithms.Keys.ToArray();

        public SignFileResponse SignFile(SignFileRequest signFileRequest)
        {
            var signFileResponse = new SignFileResponse();
            var successResult = SignFileResponseStatus.FileSigned;

            if (IsFileSigned(signFileRequest.InputFilePath))
            {
                if (signFileRequest.OverwriteSignature)
                {
                    Logger.Verbose($"File {signFileRequest.InputFilePath} is already signed, removing signature");
                    UnsignFile(signFileRequest.InputFilePath);
                    successResult = SignFileResponseStatus.FileResigned;
                }
                else
                {
                    Logger.Verbose($"File {signFileRequest.InputFilePath} is already signed, abort signing");
                    signFileResponse.Status = SignFileResponseStatus.FileAlreadySigned;
                    return signFileResponse;
                }
            }

            if (!PeSupportedHashAlgorithms.TryGetValue(
                    signFileRequest.HashAlgorithm ?? "", out var algId))
            {
                algId = PeSupportedHashAlgorithms["SHA256"];
            }

            using var signerFileInfo = new UnmanagedStruct<Win32SigningAPI.SIGNER_FILE_INFO>(new Win32SigningAPI.SIGNER_FILE_INFO
            {
                cbSize = (uint)Marshal.SizeOf<Win32SigningAPI.SIGNER_FILE_INFO>(),
                pwszFileName = signFileRequest.InputFilePath,
                hFile = IntPtr.Zero
            });
            using var dwIndex = new UnmanagedStruct<uint>(0);
            using var signerSubjectInfo = new UnmanagedStruct<Win32SigningAPI.SIGNER_SUBJECT_INFO>(
                new Win32SigningAPI.SIGNER_SUBJECT_INFO
                {
                    cbSize = (uint)Marshal.SizeOf<Win32SigningAPI.SIGNER_SUBJECT_INFO>(),
                    pdwIndex = dwIndex.Pointer,
                    dwSubjectChoice = Win32SigningAPI.SIGNER_SUBJECT_FILE,
                    union = { pSignerFileInfo = signerFileInfo.Pointer }
                });
            using var signerCertStoreInfo = new UnmanagedStruct<Win32SigningAPI.SIGNER_CERT_STORE_INFO>(
                new Win32SigningAPI.SIGNER_CERT_STORE_INFO
                {
                    cbSize = (uint)Marshal.SizeOf<Win32SigningAPI.SIGNER_CERT_STORE_INFO>(),
                    pSigningCert = signFileRequest.Certificate.Handle,
                    dwCertPolicy = Win32SigningAPI.SIGNER_CERT_POLICY_CHAIN,
                    hCertStore = IntPtr.Zero
                });
            using var signerCert = new UnmanagedStruct<Win32SigningAPI.SIGNER_CERT>(
                new Win32SigningAPI.SIGNER_CERT
                {
                    cbSize = (uint)Marshal.SizeOf<Win32SigningAPI.SIGNER_CERT>(),
                    dwCertChoice = Win32SigningAPI.SIGNER_CERT_STORE,
                    union = { pSpcChainInfo = signerCertStoreInfo.Pointer },
                    hwnd = IntPtr.Zero
                });
            using var signerSignatureInfo = new UnmanagedStruct<Win32SigningAPI.SIGNER_SIGNATURE_INFO>(
                new Win32SigningAPI.SIGNER_SIGNATURE_INFO
                {
                    cbSize = (uint)Marshal.SizeOf<Win32SigningAPI.SIGNER_SIGNATURE_INFO>(),
                    algidHash = algId.algId,
                    dwAttrChoice = Win32SigningAPI.SIGNER_NO_ATTR,
                    union = { pAttrAuthcode = IntPtr.Zero },
                    psAuthenticated = IntPtr.Zero,
                    psUnauthenticated = IntPtr.Zero
                });
            var (hr, tshr) = SignAndTimestamp(
                algId.algName,
                algId.algOid,
                signFileRequest.InputFilePath, signFileRequest.TimestampServer, signerSubjectInfo.Pointer,
                signerCert.Pointer,
                signerSignatureInfo.Pointer, signFileRequest.PrivateKey
            );

            if (hr == Win32SigningAPI.S_OK && tshr == Win32SigningAPI.S_OK)
            {
                Logger.Verbose($"{signFileRequest.InputFilePath} successfully signed");
                signFileResponse.Status = successResult;
                signFileResponse.ResultFiles = new[]
                {
                new SignFileResponseFileInfo(signFileRequest.OriginalFileName, signFileRequest.InputFilePath)
            };
            }
            else if (hr != Win32SigningAPI.S_OK)
            {
                var exception = new Win32Exception(hr);
                signFileResponse.Status = SignFileResponseStatus.FileNotSignedError;
                signFileResponse.ErrorMessage = !string.IsNullOrEmpty(exception.Message)
                    ? exception.Message
                    : $"signing file failed (0x{hr:x})";

                if ((uint)hr == 0x8007000B)
                {
                    signFileResponse.ErrorMessage =
                        $"The appxmanifest does not contain the expected publisher. Expected: <Identity ... Publisher\"{signFileRequest.Certificate.SubjectName}\" .. />.";
                }

                Logger.Error($"{signFileRequest.InputFilePath} signing failed {signFileResponse.ErrorMessage}");
            }
            else
            {
                var errorText = new Win32Exception(tshr).Message;
                signFileResponse.Status = SignFileResponseStatus.FileNotSignedError;
                signFileResponse.ErrorMessage = !string.IsNullOrEmpty(errorText)
                    ? errorText
                    : $"timestamping failed (0x{hr:x})";

                Logger.Error($"{signFileRequest.InputFilePath} timestamping failed {signFileResponse.ErrorMessage}");
            }

            return signFileResponse;
        }

        public bool IsFileSigned(string inputFileName)
        {
            using var winTrustFileInfo = new UnmanagedStruct<Win32SigningAPI.WINTRUST_FILE_INFO>(
            new Win32SigningAPI.WINTRUST_FILE_INFO
            {
                cbStruct = (uint)Marshal.SizeOf<Win32SigningAPI.WINTRUST_FILE_INFO>(),
                pcwszFilePath = inputFileName,
                hFile = IntPtr.Zero,
                pgKnownSubject = IntPtr.Zero
            });
            var winTrustData = new Win32SigningAPI.WINTRUST_DATA
            {
                cbStruct = (uint)Marshal.SizeOf<Win32SigningAPI.WINTRUST_DATA>(),
                pPolicyCallbackData = IntPtr.Zero,
                pSIPClientData = IntPtr.Zero,
                dwUIChoice = Win32SigningAPI.WinTrustDataUIChoice.None,
                fdwRevocationChecks = Win32SigningAPI.WinTrustDataRevocationChecks.None,
                dwUnionChoice = Win32SigningAPI.WinTrustDataUnionChoice.File,
                dwStateAction = Win32SigningAPI.WinTrustDataStateAction.Verify,
                hWVTStateData = IntPtr.Zero,
                pwszURLReference = IntPtr.Zero,
                dwUIContext = 0,
                union = { pFile = winTrustFileInfo.Pointer }
            };

            var actionId = new Guid(Win32SigningAPI.WINTRUST_ACTION_GENERIC_VERIFY_V2);
            var result = Win32SigningAPI.WinVerifyTrust(IntPtr.Zero, actionId, winTrustData);
            Logger.Debug($"WinVerifyTrust returned {result}");

            switch (result)
            {
                case Win32SigningAPI.WinVerifyTrustResult.Success:
                    return true;
                case Win32SigningAPI.WinVerifyTrustResult.FileNotSigned:
                    var dwLastError = (uint)Marshal.GetLastWin32Error();
                    switch (dwLastError)
                    {
                        case (uint)Win32SigningAPI.WinVerifyTrustResult.FileNotSigned:
                            return false;
                        case (uint)Win32SigningAPI.WinVerifyTrustResult.SubjectFormUnknown:
                            return true;
                        case (uint)Win32SigningAPI.WinVerifyTrustResult.ProviderUnknown:
                            return true;
                        default:
                            return false;
                    }

                case Win32SigningAPI.WinVerifyTrustResult.UntrustedRoot:
                    return true;

                case Win32SigningAPI.WinVerifyTrustResult.SubjectExplicitlyDistrusted:
                    return true;

                case Win32SigningAPI.WinVerifyTrustResult.SubjectNotTrusted:
                    return true;

                case Win32SigningAPI.WinVerifyTrustResult.LocalSecurityOption:
                    return true;

                default:
                    return false;
            }
        }

        private protected virtual (int hr, int tshr) SignAndTimestamp(
        HashAlgorithmName hashAlgorithmName,
        string timestampHashOid,
        string inputFileName,
        string timestampServer,
        /*PSIGNER_SUBJECT_INFO*/IntPtr signerSubjectInfo,
        /*PSIGNER_CERT*/IntPtr signerCert,
        /*PSIGNER_SIGNATURE_INFO*/ IntPtr signerSignatureInfo,
        AsymmetricAlgorithm privateKey)
        {
            Logger.Debug($"Call signing of {inputFileName}");

            int SignCallback(IntPtr pCertContext, IntPtr pvExtra, uint algId, byte[] pDigestToSign, uint dwDigestToSign,
                ref Win32SigningAPI.CRYPTOAPI_BLOB blob)
            {
                byte[] digest;
                try
                {
                    switch (privateKey)
                    {
                        case DSA dsa:
                            digest = dsa.CreateSignature(pDigestToSign);
                            break;
                        case ECDsa ecdsa:
                            digest = ecdsa.SignHash(pDigestToSign);
                            break;
                        case RSA rsa:
                            digest = rsa.SignHash(pDigestToSign, hashAlgorithmName, RSASignaturePadding.Pkcs1);
                            break;
                        default:
                            return Win32SigningAPI.E_INVALIDARG;
                    }
                }
                catch (Exception e)
                {
                    var hr = e.HResult != 0 ? e.HResult : Win32SigningAPI.NTE_BAD_KEY;
                    Logger.Error("Failed to sign data reporting: " + hr);
                    return hr;
                }

                var resultPtr = Marshal.AllocHGlobal(digest.Length);
                Marshal.Copy(digest, 0, resultPtr, digest.Length);
                blob.pbData = resultPtr;
                blob.cbData = (uint)digest.Length;
                return Win32SigningAPI.S_OK;
            }

            Win32SigningAPI.SignCallback callbackDelegate = SignCallback;

            using var unmanagedSignerParams = new UnmanagedStruct<Win32SigningAPI.SIGNER_SIGN_EX3_PARAMS>();
            using var unmanagedSignInfo = new UnmanagedStruct<Win32SigningAPI.SIGN_INFO>(new Win32SigningAPI.SIGN_INFO
            {
                cbSize = (uint)Marshal.SizeOf<Win32SigningAPI.SIGN_INFO>(),
                callback = Marshal.GetFunctionPointerForDelegate(callbackDelegate),
                pvOpaque = IntPtr.Zero
            });
            var signerParams = new Win32SigningAPI.SIGNER_SIGN_EX3_PARAMS
            {
                dwFlags = Win32SigningAPI.SIGN_CALLBACK_UNDOCUMENTED,
                pSubjectInfo = signerSubjectInfo,
                pSigningCert = signerCert,
                pSignatureInfo = signerSignatureInfo,
                pProviderInfo = IntPtr.Zero,
                psRequest = IntPtr.Zero,
                pCryptoPolicy = IntPtr.Zero,
                pSignCallback = unmanagedSignInfo.Pointer
            };
            unmanagedSignerParams.Fill(signerParams);

            var hr = Win32SigningAPI.SignerSignEx3(
                signerParams.dwFlags,
                signerParams.pSubjectInfo,
                signerParams.pSigningCert,
                signerParams.pSignatureInfo,
                signerParams.pProviderInfo,
                signerParams.dwTimestampFlags,
                signerParams.pszTimestampAlgorithmOid,
                signerParams.pwszTimestampURL,
                signerParams.psRequest,
                IntPtr.Zero,
                signerParams.pSignerContext,
                signerParams.pCryptoPolicy,
                signerParams.pSignCallback,
                signerParams.pReserved
            );

            if (signerParams.pSignerContext != IntPtr.Zero)
            {
                var signerContext = new IntPtr();
                Marshal.PtrToStructure(signerParams.pSignerContext, signerContext);
                Win32SigningAPI.SignerFreeSignerContext(signerContext);
            }

            var tshr = Win32SigningAPI.S_OK;
            if (hr == Win32SigningAPI.S_OK && !string.IsNullOrWhiteSpace(timestampServer))
            {
                Logger.Verbose($"Timestamping with url {timestampServer}");
                var timestampRetries = 5;
                do
                {
                    tshr = timestampHashOid == Win32SigningAPI.OID_OIWSEC_SHA1
                        ? Win32SigningAPI.SignerTimeStamp(signerSubjectInfo, timestampServer)
                        : Win32SigningAPI.SignerTimeStampEx2(
                            Win32SigningAPI.SIGNER_TIMESTAMP_RFC3161,
                            signerSubjectInfo,
                            timestampServer,
                            timestampHashOid,
                            IntPtr.Zero,
                            IntPtr.Zero,
                            IntPtr.Zero
                        );
                    if (tshr == Win32SigningAPI.S_OK)
                    {
                        Logger.Verbose("Timestamping succeeded");
                    }
                    else
                    {
                        Logger.Error($"Timestamping failed with {tshr}, retries: {timestampRetries}");
                        Thread.Sleep(1000);
                    }
                } while (tshr != Win32SigningAPI.S_OK && (timestampRetries--) > 0);
            }

            return (hr, tshr);
        }

        public virtual void UnsignFile(string fileName)
        {
            using var file = new FileStream(fileName, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
            // TODO: remove multiple certificates here?
            if (Win32SigningAPI.ImageEnumerateCertificates(file.SafeFileHandle, Win32SigningAPI.CERT_SECTION_TYPE_ANY,
                    out var dwNumCerts) &&
                dwNumCerts == 1)
            {
                Win32SigningAPI.ImageRemoveCertificate(file.SafeFileHandle, 0);
            }
        }
    }
}
