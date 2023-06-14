using Spartacus.ProcMon;
using Spartacus.Spartacus.CommandLine;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Spartacus.ProcMon.ProcMonConstants;

namespace Spartacus.Modes.COM
{
    class ModeCOM : ModeBase
    {
        public override void Run()
        {
            if (!RuntimeData.IsExistingLog)
            {
                GatherEvents();
            }
            else
            {
                Logger.Info("Processing existing log file: " + RuntimeData.PMLFile);
            }

            Logger.Info("Reading events file...");
            ProcMonPML log = new(RuntimeData.PMLFile);

            Logger.Info("Found " + String.Format("{0:N0}", log.TotalEvents()) + " events...");

            // Find all events of interest, like DLLs that weren't loaded etc.
            Stopwatch watch = Stopwatch.StartNew();
            Dictionary<string, PMLEvent> events = FindInterestingEvents(log);
            watch.Stop();
            Logger.Debug(String.Format("FindEvents() took {0:N0}ms", watch.ElapsedMilliseconds));

            // Save output to CSV.
            do
            {
                try
                {
                    ExportToCSV(events);
                    break;  // Saved successfully.
                }
                catch (Exception e)
                {
                    Logger.Error(e.Message);
                    Logger.Warning("There was an error saving the output. In order to avoid losing the processed data");
                    Logger.Warning("we're going to give it another go. When you resolve the error described above");
                    Logger.Warning("hit ENTER and another attempt at saving the output will be made.", false, true);
                    Console.ReadLine();
                    Logger.Warning("Trying to save file again...");
                }
            } while (true);
        }

        public override void SanitiseAndValidateRuntimeData()
        {
            if (RuntimeData.IsExistingLog)
            {
                SanitiseExistingLogProcessing();
            }
            else
            {
                SanitiseNewLogProcessing();
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

            // Exports folder.
            if (String.IsNullOrEmpty(RuntimeData.ExportsDirectory))
            {
                Logger.Debug("--exports is missing, will skip DLL proxy generation");
            }
            else if (Directory.Exists(RuntimeData.ExportsDirectory))
            {
                Logger.Debug("--exports directory already exists");
            }
            else
            {
                Logger.Debug("--exports directory does not exist - creating now");
                // If this goes wrong, it will throw an exception.
                Directory.CreateDirectory(RuntimeData.ExportsDirectory);
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

        protected void GatherEvents()
        {
            ProcMonManager procMon = new(RuntimeData.ProcMonExecutable);

            Logger.Verbose("Making sure there are no ProcessMonitor instances...");
            procMon.Terminate();

            if (!String.IsNullOrEmpty(RuntimeData.PMLFile) && File.Exists(RuntimeData.PMLFile))
            {
                Logger.Verbose("Deleting previous log file: " + RuntimeData.PMLFile);
                File.Delete(RuntimeData.PMLFile);
            }

            Logger.Info("Getting PMC file...");
            RuntimeData.PMCFile = procMon.CreateConfigForCOM(RuntimeData.PMCFile, RuntimeData.InjectBackingFileIntoConfig, RuntimeData.PMLFile);

            Logger.Info("Starting ProcessMonitor...");
            procMon.Start(RuntimeData.PMCFile);

            Logger.Info("Process Monitor has started...");

            Logger.Warning("Press ENTER when you want to terminate Process Monitor and parse its output...", false, true);
            Console.ReadLine();

            Logger.Info("Terminating Process Monitor...");
            procMon.Terminate();
        }

        protected Dictionary<string, PMLEvent> FindInterestingEvents(ProcMonPML log)
        {
            UInt32 counter = 0;
            UInt32 steps = log.TotalEvents() / 10;
            if (steps == 0)
            {
                steps = 1;
            }

            Dictionary<string, PMLEvent> events = new Dictionary<string, PMLEvent>();

            Logger.Info("Searching events...", false, true);
            log.Rewind();
            do
            {
                if (++counter % steps == 0)
                {
                    Logger.Info(".", false, false);
                }

                // Get the next event from the stream.
                PMLEvent e = log.GetNextEvent().GetValueOrDefault();
                if (!e.Loaded)
                {
                    break;
                }

                // We want a "RegOpenKey" and "NAME NOT FOUND".
                if (e.EventClass != EventClassType.Registry)
                {
                    continue;
                }
                else if (e.Result != EventResult.NAME_NOT_FOUND)
                {
                    continue;
                }
                else if (e.RegistryOperation != EventRegistryOperation.RegOpenKey)
                {
                    continue;
                }

                // Ignore paths that don't end in "InprocServer32"
                string p = e.Path.ToLower();
                if (!p.EndsWith("inprocserver32"))
                {
                    continue;
                }

                // Avoid duplicates.
                if (events.ContainsKey(p))
                {
                    continue;
                }

                events.Add(p, e);
            } while (true);

            Logger.Info("", true, false);
            Logger.Info("Found " + String.Format("{0:N0}", events.Count()) + " events of interest...");

            return events;
        }

        protected void ExportToCSV(Dictionary<string, PMLEvent> events)
        {
            Logger.Info("Saving to CSV...");
            using (StreamWriter stream = File.CreateText(RuntimeData.CSVFile))
            {
                stream.WriteLine(string.Format("Process, Image Path, Affected Path, Integrity, Command Line"));
                foreach (KeyValuePair<string, PMLEvent> item in events)
                {
                    stream.WriteLine(
                        string.Format(
                            "\"{0}\",\"{1}\",\"{2}\",\"{3}\",\"{4}\"",
                            item.Value.Process.ProcessName,
                            item.Value.Process.ImagePath,
                            item.Value.Path,
                            item.Value.Process.Integrity,
                            item.Value.Process.CommandLine.Replace("\"", "\"\""))
                        );
                }
            }
        }
    }
}
