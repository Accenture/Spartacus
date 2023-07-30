using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Spartacus.Modes.SIGN.MsSign
{
    internal static class Win32SigningAPI
    {
        public const uint SIGN_CALLBACK_UNDOCUMENTED = 0x400;

        public const string OID_OIWSEC_SHA1 = "1.3.14.3.2.26";
        public const string OID_RSA_MD5 = "1.2.840.113549.2.5";
        public const string OID_OIWSEC_SHA256 = "2.16.840.1.101.3.4.2.1";
        public const string OID_OIWSEC_SHA384 = "2.16.840.1.101.3.4.2.2";
        public const string OID_OIWSEC_SHA512 = "2.16.840.1.101.3.4.2.3";
        public const uint SIGNER_TIMESTAMP_RFC3161 = 2;

        public const int S_OK = 0;

        public const uint SIGNER_NO_ATTR = 0;
        public const uint SIGNER_CERT_STORE = 2;

        public const uint SIGNER_CERT_POLICY_CHAIN = 2;

        public const uint SIGNER_SUBJECT_FILE = 1;
        public const int E_INVALIDARG = unchecked((int)0x80070057);

        public const string WINTRUST_ACTION_GENERIC_VERIFY_V2 = "{00AAC56B-CD44-11d0-8CC2-00C04FC295EE}";

        public const uint ALG_CLASS_HASH = (4 << 13);
        public const uint ALG_TYPE_ANY = (0);

        public const uint ALG_SID_SHA1 = 4;
        public const uint ALG_SID_MD5 = 3;
        public const uint ALG_SID_SHA_256 = 12;
        public const uint ALG_SID_SHA_384 = 13;
        public const uint ALG_SID_SHA_512 = 14;

        public const uint CALG_SHA1 = ALG_CLASS_HASH | ALG_TYPE_ANY | ALG_SID_SHA1;
        public const uint CALG_MD5 = ALG_CLASS_HASH | ALG_TYPE_ANY | ALG_SID_MD5;
        public const uint CALG_SHA_256 = ALG_CLASS_HASH | ALG_TYPE_ANY | ALG_SID_SHA_256;
        public const uint CALG_SHA_384 = ALG_CLASS_HASH | ALG_TYPE_ANY | ALG_SID_SHA_384;
        public const uint CALG_SHA_512 = ALG_CLASS_HASH | ALG_TYPE_ANY | ALG_SID_SHA_512;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct SIGNER_CERT
        {
            public uint cbSize;
            public uint dwCertChoice;
            public SIGNER_CERT_UNION union;
            public IntPtr hwnd;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct SIGNER_SIGNATURE_INFO
        {
            public uint cbSize;
            public uint algidHash;
            public uint dwAttrChoice;
            public SIGNER_SIGNATURE_INFO_UNION union;
            public /*PCRYPT_ATTRIBUTES*/ IntPtr psAuthenticated;
            public /*PCRYPT_ATTRIBUTES*/ IntPtr psUnauthenticated;
        }

        [StructLayout(LayoutKind.Explicit, CharSet = CharSet.Unicode)]
        public struct SIGNER_SIGNATURE_INFO_UNION
        {
            [FieldOffset(0)] public /*PSIGNER_ATTR_AUTHCODE*/ IntPtr pAttrAuthcode;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SIGNER_CERT_STORE_INFO
        {
            public uint cbSize;
            public /*PCERT_CONTEXT*/ IntPtr pSigningCert;
            public uint dwCertPolicy;
            public IntPtr hCertStore;
        }

        [StructLayout(LayoutKind.Explicit, CharSet = CharSet.Unicode)]
        public struct SIGNER_CERT_UNION
        {
            [FieldOffset(0)] public /*PSIGNER_CERT_STORE_INFO*/ IntPtr pSpcChainInfo;
        }

        [StructLayout(LayoutKind.Explicit, CharSet = CharSet.Unicode)]
        public struct SIGNER_SUBJECT_INFO_UNION
        {
            [FieldOffset(0)] public /*PSIGNER_FILE_INFO*/ IntPtr pSignerFileInfo;
            // [FieldOffset(0)]
            // public SIGNER_BLOB_INFO* pSignerBlobInfo;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct SIGNER_FILE_INFO
        {
            public uint cbSize;
            public string pwszFileName;
            public IntPtr hFile;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct SIGNER_SUBJECT_INFO
        {
            public uint cbSize;
            public IntPtr pdwIndex;
            public uint dwSubjectChoice;
            public SIGNER_SUBJECT_INFO_UNION union;
        }

        [DllImport("mssign32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int SignerSignEx3(
            [In] uint dwFlags,
            [In] /*PSIGNER_SUBJECT_INFO*/ IntPtr pSubjectInfo,
            [In] /*PSIGNER_CERT*/ IntPtr pSignerCert,
            [In] /*PSIGNER_SIGNATURE_INFO*/ IntPtr pSignatureInfo,
            [In, Optional] /*PSIGNER_PROVIDER_INFO*/ IntPtr pProviderInfo,
            [In, Optional] uint dwTimestampFlags,
            [In, Optional, MarshalAs(UnmanagedType.LPStr)]
        string pszAlgorithmOid,
            [In, Optional] string pwszTimestampURL,
            [In, Optional] /*PCRYPT_ATTRIBUTES*/ IntPtr psRequest,
            [In, Optional] IntPtr pSipData,
            [Out] /*PPSIGNER_CONTEXT*/IntPtr ppSignerContext,
            [In, Optional] IntPtr pCryptoPolicy,
            [In] /*SIGN_INFO*/IntPtr pSignInfo,
            [Optional] IntPtr pReserved
        );

        [StructLayout(LayoutKind.Sequential)]
        public struct SIGN_INFO
        {
            public uint cbSize;
            public IntPtr callback;
            public IntPtr pvOpaque;
        }

        [DllImport("mssign32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int SignerFreeSignerContext(
            [In] /*PSIGNER_CONTEXT*/ IntPtr pSignerContext
        );

        [DllImport("mssign32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int SignerTimeStamp(
            [In] /*PSIGNER_SUBJECT_INFO*/ IntPtr pSubjectInfo,
            [In] string pwszHttpTimeStamp,
            [In, Optional] /*PCRYPT_ATTRIBUTES*/ IntPtr psRequest,
            [In, Optional] IntPtr pSipData
        );

        [DllImport("mssign32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int SignerTimeStampEx2(
            [In] uint dwFlags,
            [In] /*PSIGNER_SUBJECT_INFO*/ IntPtr pSubjectInfo,
            [In] string pwszHttpTimeStamp,
            [In, MarshalAs(UnmanagedType.LPStr)] string dwAlgId,
            [In, Optional] /*PCRYPT_ATTRIBUTES*/ IntPtr psRequest,
            [In, Optional] IntPtr pSipData,
            [Out] /*PPSIGNER_CONTEXT*/IntPtr ppSignerContext
        );


        public enum WinVerifyTrustResult : uint
        {
            Success = 0,
            ProviderUnknown = 0x800b0001, // Trust provider is not recognized on this system
            SubjectFormUnknown = 0x800b0003, // Trust provider does not support the form specified for the subject
            SubjectNotTrusted = 0x800b0004, // Subject failed the specified verification action
            FileNotSigned = 0x800B0100, // TRUST_E_NOSIGNATURE - File was not signed
            SubjectExplicitlyDistrusted = 0x800B0111, // Signer's certificate is in the Untrusted Publishers store

            UntrustedRoot =
                0x800B0109, // CERT_E_UNTRUSTEDROOT - A certification chain processed correctly but terminated in a root certificate that is not trusted by the trust provider.

            LocalSecurityOption =
                0x80092026 // CRYPT_E_SECURITY_SETTINGS
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct WINTRUST_DATA
        {
            public uint cbStruct;
            public IntPtr pPolicyCallbackData;
            public IntPtr pSIPClientData;
            public WinTrustDataUIChoice dwUIChoice;
            public WinTrustDataRevocationChecks fdwRevocationChecks;
            public WinTrustDataUnionChoice dwUnionChoice;
            public WINTRUST_DATA_UNION union;
            public WinTrustDataStateAction dwStateAction;
            public IntPtr hWVTStateData;
            public IntPtr pwszURLReference;
            public uint dwProvFlags;
            public uint dwUIContext;
        }

        public enum WinTrustDataStateAction : uint
        {
            Verify = 0x00000001
        }

        [StructLayout(LayoutKind.Explicit, CharSet = CharSet.Unicode)]
        public struct WINTRUST_DATA_UNION
        {
            [FieldOffset(0)] public /*PWINTRUST_FILE_INFO*/ IntPtr pFile; // individual file
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct WINTRUST_FILE_INFO
        {
            public uint cbStruct;
            public string pcwszFilePath;
            public IntPtr hFile;
            public IntPtr pgKnownSubject;
        }

        public enum WinTrustDataUIChoice : uint
        {
            None = 2
        }

        public enum WinTrustDataRevocationChecks : uint
        {
            None = 0x00000000
        }

        public enum WinTrustDataUnionChoice : uint
        {
            File = 1
        }

        [DllImport("wintrust.dll", ExactSpelling = true, SetLastError = false, CharSet = CharSet.Unicode)]
        public static extern WinVerifyTrustResult WinVerifyTrust(
            [In] IntPtr hwnd,
            [In] [MarshalAs(UnmanagedType.LPStruct)]
        Guid pgActionID,
            [In] WINTRUST_DATA pWVTData
        );

        [DllImport("imagehlp.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ImageEnumerateCertificates(
            [In] SafeFileHandle FileHandle,
            [In] uint TypeFilter,
            [Out] out uint CertificateCount,
            [In, Out, Optional] uint[] Indices,
            [In, Optional] uint IndexCount
        );

        public const uint CERT_SECTION_TYPE_ANY = 0xFF;

        [DllImport("imagehlp.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ImageRemoveCertificate(SafeFileHandle fileHandle, uint index);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct SIGNER_SIGN_EX3_PARAMS
        {
            public uint dwFlags;
            public /*PSIGNER_SUBJECT_INFO*/ IntPtr pSubjectInfo;
            public /*PSIGNER_CERT*/ IntPtr pSigningCert;
            public /*PSIGNER_SIGNATURE_INFO*/ IntPtr pSignatureInfo;
            public /*PSIGNER_PROVIDER_INFO*/ IntPtr pProviderInfo;
            public uint dwTimestampFlags;
            [MarshalAs(UnmanagedType.LPStr)] public string pszTimestampAlgorithmOid;
            [MarshalAs(UnmanagedType.LPWStr)] public string pwszTimestampURL;
            public IntPtr psRequest;
            public /*PSIGN_INFO*/ IntPtr pSignCallback;
            public /*PPSIGNER_CONTEXT*/ IntPtr pSignerContext;
            public IntPtr pCryptoPolicy;
            public IntPtr pReserved;
        }

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        public delegate int SignCallback(
            [In, MarshalAs(UnmanagedType.SysInt)] IntPtr pCertContext,
            [In, MarshalAs(UnmanagedType.SysInt)] IntPtr pvExtra,
            [In, MarshalAs(UnmanagedType.U4)] uint algId,
            [In, MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U1, SizeParamIndex = 4)]
        byte[] pDigestToSign,
            [In, MarshalAs(UnmanagedType.U4)] uint dwDigestToSign,
            [In, Out] ref CRYPTOAPI_BLOB blob
        );

        [StructLayout(LayoutKind.Sequential)]
        public struct CRYPTOAPI_BLOB
        {
            public uint cbData;
            public IntPtr pbData;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct APPX_SIP_CLIENT_DATA
        {
            public /*PSIGNER_SIGN_EX2_PARAMS or PSIGNER_SIGN_EX3_PARAMS*/ IntPtr pSignerParams;
            public /*LPVOID*/ IntPtr pAppxSipState;
        }

        public const int NTE_BAD_KEY = unchecked((int)0x80090003);
        public const int TRUST_E_SUBJECT_FORM_UNKNOWN = unchecked((int)0x800B0003);
        public const int TRUST_E_BAD_DIGEST = unchecked((int)0x80096010);
        public const uint LOAD_LIBRARY_AS_DATAFILE = 0x00000002;
        public const string szOID_OIWSEC_sha1 = "1.3.14.3.2.26";
        public const string szOID_NIST_sha256 = "2.16.840.1.101.3.4.2.1";

        [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern int SetDllDirectoryW(string strPathName);

        [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr LoadLibraryExW(string strFileName, IntPtr hFile, uint ulFlags);

        [DllImport("Kernel32.dll", SetLastError = true)]
        public static extern bool FreeLibrary(IntPtr hModule);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct CRYPT_DATA_BLOB
        {
            internal uint cbData;
            internal IntPtr pbData;
        }

        [DllImport("clr.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int _AxlPublicKeyBlobToPublicKeyToken(
            [In] ref CRYPT_DATA_BLOB pCspPublicKeyBlob,
            [In, Out] ref IntPtr ppwszPublicKeyToken);


        [DllImport("clr.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int _AxlGetIssuerPublicKeyHash(
            [In] IntPtr pCertContext,
            [In, Out] ref IntPtr ppwszPublicKeyHash);

        [DllImport("clr.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int CertTimestampAuthenticodeLicense(
            [In] ref CRYPT_DATA_BLOB pSignedLicenseBlob,
            [In] string pwszTimestampURI,
            [In, Out] ref CRYPT_DATA_BLOB pTimestampSignatureBlob);

        [DllImport("Kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool HeapFree(
            [In] IntPtr hHeap,
            [In] uint dwFlags,
            [In] IntPtr lpMem);

        [DllImport("Kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr GetProcessHeap();

        [StructLayout(LayoutKind.Sequential)]
        public struct CRYPT_TIMESTAMP_PARA
        {
            public IntPtr pszTSAPolicyId;
            public bool fRequestCerts;
            public CRYPTOAPI_BLOB Nonce;
            public int cExtension;
            public IntPtr rgExtension;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CRYPT_TIMESTAMP_CONTEXT
        {
            public uint cbEncoded;
            public IntPtr pbEncoded;
            public IntPtr pTimeStamp;
        }

        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [DllImport("crypt32.dll", CallingConvention = CallingConvention.Winapi)]
        public static extern void CryptMemFree(IntPtr pv);

        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [DllImport("crypt32.dll", CallingConvention = CallingConvention.Winapi, SetLastError = true)]
        public static extern bool CertFreeCertificateContext(IntPtr pCertContext);

        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [DllImport("crypt32.dll", CallingConvention = CallingConvention.Winapi, SetLastError = true)]
        public static extern bool CertCloseStore(IntPtr pCertContext, int dwFlags);

        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [DllImport("crypt32.dll", CallingConvention = CallingConvention.Winapi, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CryptRetrieveTimeStamp(
            [In][MarshalAs(UnmanagedType.LPWStr)] string wszUrl,
            [In] uint dwRetrievalFlags,
            [In] int dwTimeout,
            [In][MarshalAs(UnmanagedType.LPStr)] string pszHashId,
            [In, Out] ref CRYPT_TIMESTAMP_PARA pPara,
            [In] byte[] pbData,
            [In] int cbData,
            [In, Out] ref IntPtr ppTsContext,
            [In, Out] ref IntPtr ppTsSigner,
            [In, Out] ref IntPtr phStore);
    }
}
