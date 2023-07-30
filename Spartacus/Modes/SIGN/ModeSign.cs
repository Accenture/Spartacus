using Spartacus.Spartacus.CommandLine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Spartacus.Modes.SIGN
{
    class ModeSign : ModeBase
    {
        public override void Run()
        {
            switch (RuntimeData.Action.ToLower())
            {
                case "generate":
                    GenerateSelfSignedCertificate();
                    break;
                case "sign":
                default:
                    SignFile();
                    break;
            }
        }

        public override void SanitiseAndValidateRuntimeData()
        {
            switch (RuntimeData.Action.ToLower())
            {
                case "generate":
                    SanitiseCertificateGeneration();
                    break;
                case "sign":
                default:
                    SanitiseFileSigning();
                    break;
            }
        }

        protected void SanitiseCertificateGeneration()
        {
            CertificateManager certManager = new();

            // Check the PFX file.
            if (String.IsNullOrEmpty(RuntimeData.Certificate.PFXFile))
            {
                throw new Exception("--pfx is missing");
            }
            else if (File.Exists(RuntimeData.Certificate.PFXFile) && !RuntimeData.Overwrite)
            {
                throw new Exception("--pfx already exists and --overwrite has not been passed as an argument");
            }

            // Check PFX Password.
            if (String.IsNullOrEmpty(RuntimeData.Certificate.Password))
            {
                throw new Exception("--password is missing or is empty");
            }

            string copyIssuer = "";
            string copySubject = "";
            // Check certificate issuer/subject.
            if (!String.IsNullOrEmpty(RuntimeData.Certificate.CopyFrom))
            {
                if (!File.Exists(RuntimeData.Certificate.CopyFrom))
                {
                    throw new Exception("--copy-from does not exist: " +  RuntimeData.Certificate.CopyFrom);
                }

                try
                {
                    X509Certificate existingCertificate = certManager.GetCertificateFromFile(RuntimeData.Certificate.CopyFrom);
                    copyIssuer = existingCertificate.Issuer;
                    copySubject = existingCertificate.Subject;
                }
                catch (Exception e)
                {
                    Logger.Error(e.Message);
                    throw new Exception("Could not read certificate from: " + RuntimeData.Certificate.CopyFrom + " - Is the file signed?");
                }
            }

            if (String.IsNullOrEmpty(RuntimeData.Certificate.Issuer))
            {
                if (String.IsNullOrEmpty(copyIssuer))
                {
                    throw new Exception("Certificate Issuer is empty");
                }
                RuntimeData.Certificate.Issuer = copyIssuer;
            }

            if (String.IsNullOrEmpty(RuntimeData.Certificate.Subject))
            {
                if (String.IsNullOrEmpty(copySubject))
                {
                    throw new Exception("Certificate Subject is empty");
                }
                RuntimeData.Certificate.Subject = copySubject;
            }

            // Check NotBefore and NotAfter.
            if (RuntimeData.Certificate.NotBefore == default)
            {
                throw new Exception("Invalid --not-before date");
            }
            else if (RuntimeData.Certificate.NotAfter == default)
            {
                throw new Exception("Invalid --not-after date");
            }

            // Check date validity.
            if (RuntimeData.Certificate.NotBefore > DateTime.Now)
            {
                Logger.Warning("--not-before is in the future, this may cause signing issues");
            }

            if (RuntimeData.Certificate.NotAfter < DateTime.Now)
            {
                Logger.Warning("--not-after is in the past, this may cause signing issues");
            }

            Logger.Debug("New Certificate Subject: " + RuntimeData.Certificate.Subject);
            Logger.Debug("New Certificate Issuer: " + RuntimeData.Certificate.Issuer);
            Logger.Debug("New Certificate NotBefore: " + RuntimeData.Certificate.NotBefore);
            Logger.Debug("New Certificate NotAfter: " + RuntimeData.Certificate.NotAfter);
            Logger.Debug("New Certificate Password: " + RuntimeData.Certificate.Password);
            Logger.Debug("New Certificate PfxFile: " + RuntimeData.Certificate.PFXFile);
        }

        protected void SanitiseFileSigning()
        {
            CertificateManager certManager = new();

            // Check the PFX file.
            if (String.IsNullOrEmpty(RuntimeData.Certificate.PFXFile))
            {
                throw new Exception("--pfx is missing");
            }
            else if (!File.Exists(RuntimeData.Certificate.PFXFile) && !RuntimeData.Overwrite)
            {
                throw new Exception("--pfx does not exist: " + RuntimeData.Certificate.PFXFile);
            }

            // Check PFX Password.
            if (String.IsNullOrEmpty(RuntimeData.Certificate.Password))
            {
                throw new Exception("--password is missing or is empty");
            }

            if (String.IsNullOrEmpty(RuntimeData.Path))
            {
                throw new Exception("--path is missing");
            }
            else if (!File.Exists(RuntimeData.Path))
            {
                throw new Exception("--path does not exist: " + RuntimeData.Path);
            }

            if (String.IsNullOrEmpty(RuntimeData.Certificate.Timestamp))
            {
                RuntimeData.Certificate.Timestamp = ""; // Make sure it's not null.
            }

            if (String.IsNullOrEmpty(RuntimeData.Certificate.Algorithm))
            {
                throw new Exception("--algorithm is missing");
            }
            else if (!IsValidSignatureAlgorithm(RuntimeData.Certificate.Algorithm))
            {
                throw new Exception("--algorithm is invalid");
            }
        }

        protected bool IsValidSignatureAlgorithm(string algorithm)
        {
            List<string> validAlgorithms = new() { "MD5", "SHA1", "SHA256", "SHA384", "SHA512" };
            return validAlgorithms.Contains(algorithm);
        }

        protected void GenerateSelfSignedCertificate()
        {
            CertificateManager certManager = new();

            Logger.Info("Generating self-signed certificate...");

            try
            {
                bool result = certManager.GenerateCertificate(RuntimeData.Certificate.Subject, RuntimeData.Certificate.Issuer, RuntimeData.Certificate.NotBefore, RuntimeData.Certificate.NotAfter, RuntimeData.Certificate.Password, RuntimeData.Certificate.PFXFile);
                if (!result)
                {
                    Logger.Error("Could not generate self-signed certificate");
                    return;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                return;
            }

            Logger.Info("Certificate generated at " + RuntimeData.Certificate.PFXFile + ", with password " + RuntimeData.Certificate.Password);
        }

        protected void SignFile()
        {
            CertificateManager certManager = new();

            Logger.Info("Signing file...");
            try
            {
                bool result = certManager.SignFile(RuntimeData.Certificate.PFXFile, RuntimeData.Certificate.Password, RuntimeData.Path, RuntimeData.Certificate.Algorithm, RuntimeData.Certificate.Timestamp);
                if (!result)
                {
                    Logger.Error("Could not sign file");
                    return;
                }
            }
            catch (Exception e)
            {
                Logger.Error("Could not sign file");
                Logger.Error(e.Message);
                return;
            }

            Logger.Info("File signed: " + RuntimeData.Path);
        }
    }
}
