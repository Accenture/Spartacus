using Spartacus.ProcMon;
using Spartacus.Spartacus.CommandLine;
using System;
using System.IO;


namespace Spartacus.Modes.COM
{
    class ModeCOM : ModeBase
    {
        public override void Run()
        {
            if (RuntimeData.isACL)
            {
                ACLExecution execution = new();
                execution.Run();
            }
            else
            {
                StandardExecution execution = new();
                execution.Run();
            }
        }

        public override void SanitiseAndValidateRuntimeData()
        {
            if (!RuntimeData.isACL)
            {
                if (RuntimeData.IsExistingLog)
                {
                    SanitiseExistingLogProcessing();
                }
                else
                {
                    SanitiseNewLogProcessing();
                }
            }

            // Check for CSV output file.
            if (String.IsNullOrEmpty(RuntimeData.CSVFile))
            {
                throw new Exception("--csv is missing");
            }
            else if (File.Exists(RuntimeData.CSVFile))
            {
                Logger.Debug("--csv exists and will be overwritten");
            }
        }

        protected void SanitiseExistingLogProcessing()
        {
            // Check if the PML file exists.
            if (String.IsNullOrEmpty(RuntimeData.PMLFile))
            {
                throw new Exception("--pml is missing");
            }
            else if (!File.Exists(RuntimeData.PMLFile))
            {
                throw new Exception("--pml does not exist: " + RuntimeData.PMLFile);
            }
        }

        protected void SanitiseNewLogProcessing()
        {
            // Check for ProcMon.
            if (String.IsNullOrEmpty(RuntimeData.ProcMonExecutable))
            {
                throw new Exception("--procmon is missing");
            }
            else if (!File.Exists(RuntimeData.ProcMonExecutable))
            {
                throw new Exception("--procmon does not exist: " + RuntimeData.ProcMonExecutable);
            }

            // Check for ProcMon config & log file.
            if (String.IsNullOrEmpty(RuntimeData.PMCFile))
            {
                // Since no --pmc has been passed, it means that we can't load the --pml file automatically from
                // that configuration. This means that we _must_ have --pml passed here.
                if (String.IsNullOrEmpty(RuntimeData.PMLFile))
                {
                    throw new Exception("--pml is missing");
                }
                else if (File.Exists(RuntimeData.PMLFile))
                {
                    // Just a debug statement.
                    Logger.Debug("--pml exists and will be overwritten");
                }
            }
            else if (!File.Exists(RuntimeData.PMCFile))
            {
                // If the argument was passed but does not exist, exit.
                throw new Exception("--pmc does not exist: " + RuntimeData.PMCFile);
            }
            else
            {
                // If we reach this, it means that --pmc has been passed through and exists.
                ProcMonPMC pmc = new(RuntimeData.PMCFile);

                // If the existing PMC file has no logfile/backing file, check to see if --pml has been set.
                if (String.IsNullOrEmpty(pmc.GetConfiguration().Logfile))
                {
                    if (String.IsNullOrEmpty(RuntimeData.PMLFile))
                    {
                        throw new Exception("The passed --pmc file that has no log/backing file configured and no --pml argument was passed to set it. " +
                            "Either setup the backing file in the existing PMC file or pass a --pml parameter");
                    }

                    // Here, the --pmc config has no PML path for log/backing, but we've passed a --pml argument.
                    // Therefore, we'll inject our new PML location into the existing PMC config.
                    RuntimeData.InjectBackingFileIntoConfig = true;
                }
                else
                {
                    // The PMC file has a backing file, so we don't need the --pml argument.
                    RuntimeData.PMLFile = pmc.GetConfiguration().Logfile;
                }
            }
        }
    }
}
