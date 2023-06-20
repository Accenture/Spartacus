using Microsoft.Win32;
using Spartacus.ProcMon;
using Spartacus.Spartacus.CommandLine;
using Spartacus.Utils;
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
    class StandardExecution
    {
        Helper Helper = new();

        public struct COMFinding
        {
            public string ProcessName;
            public string ImagePath;
            public string MissingRegistryPath;
            public string Integrity;
            public string CommandLine;
            public string ExistingRegistryPath;
            public string ExistingRegistryDescription;
            public string ExistingRegistryCOM;
        }

        public void Run()
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

            Logger.Verbose("Found " + String.Format("{0:N0}", log.TotalEvents()) + " events...");

            // Find all events of interest, like DLLs that weren't loaded etc.
            Stopwatch watch = Stopwatch.StartNew();
            Dictionary<string, PMLEvent> events = FindInterestingEvents(log);
            watch.Stop();
            Logger.Debug(String.Format("FindEvents() took {0:N0}ms", watch.ElapsedMilliseconds));

            Logger.Verbose("Identifying existing COM entries...");
            Dictionary<string, COMFinding> findings = MapRegistryEvents(events);

            // Save output to CSV.
            do
            {
                try
                {
                    ExportToCSV(findings);
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

        protected void GatherEvents()
        {
            if (!Helper.UserACL.IsElevated())
            {
                Logger.Warning("Procmon execution requires elevated permissions - brace yourself for a UAC prompt.");
            }

            ProcMonManager procMon = new(RuntimeData.ProcMonExecutable);

            Logger.Verbose("Making sure there are no ProcessMonitor instances...");
            procMon.Terminate();

            if (!String.IsNullOrEmpty(RuntimeData.PMLFile) && File.Exists(RuntimeData.PMLFile))
            {
                Logger.Verbose("Deleting previous log file: " + RuntimeData.PMLFile);
                File.Delete(RuntimeData.PMLFile);
            }

            Logger.Verbose("Getting PMC file...");
            RuntimeData.PMCFile = procMon.CreateConfigForCOM(RuntimeData.PMCFile, RuntimeData.InjectBackingFileIntoConfig, RuntimeData.PMLFile);

            Logger.Info("Starting ProcessMonitor...");
            procMon.Start(RuntimeData.PMCFile);

            Logger.Verbose("Process Monitor has started...");

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

        protected Dictionary<string, COMFinding> MapRegistryEvents(Dictionary<string, PMLEvent> events)
        {
            Dictionary<string, COMFinding> findings = new();

            foreach (KeyValuePair<string, PMLEvent> item in events) {
                string guid = Helper.ExtractGUIDFromString(item.Key);
                if (String.IsNullOrEmpty(guid))
                {
                    continue;
                }
                else if (findings.ContainsKey(guid))
                {
                    continue;
                }

                COMFinding finding = new()
                {
                    ProcessName = item.Value.Process.ProcessName,
                    ImagePath = item.Value.Process.ImagePath,
                    MissingRegistryPath = item.Value.Path,
                    Integrity = item.Value.Process.Integrity,
                    CommandLine = item.Value.Process.CommandLine
                };

                try
                {
                    RegistryKey CLSID = Registry.ClassesRoot.OpenSubKey(@$"CLSID\{guid}", false);
                    if (CLSID != null)
                    {
                        finding.ExistingRegistryPath = CLSID.Name;
                        finding.ExistingRegistryDescription = CLSID.GetValue("")?.ToString();
                        finding.ExistingRegistryCOM = CLSID.OpenSubKey("InProcServer32")?.GetValue("")?.ToString();
                    }
                }
                catch (Exception e)
                {
                    // Probably missing keys.
                }

                findings.Add(guid, finding);
            }

            return findings;
        }

        protected void ExportToCSV(Dictionary<string, COMFinding> findings)
        {
            Logger.Info("Saving to CSV...");
            using (StreamWriter stream = File.CreateText(RuntimeData.CSVFile))
            {
                stream.WriteLine(string.Format("Process,Image Path,Missing Registry Path,Integrity,Command Line,Existing CLSID Path,Existing CLSID Description,Existing CLSID File"));
                foreach (KeyValuePair<string, COMFinding> item in findings)
                {
                    stream.WriteLine(
                        string.Format(
                            "\"{0}\",\"{1}\",\"{2}\",\"{3}\",\"{4}\",\"{5}\",\"{6}\",\"{7}\"",
                            !String.IsNullOrEmpty(item.Value.ProcessName) ? item.Value.ProcessName : "",
                            !String.IsNullOrEmpty(item.Value.ImagePath) ? item.Value.ImagePath : "",
                            !String.IsNullOrEmpty(item.Value.MissingRegistryPath) ? item.Value.MissingRegistryPath : "",
                            !String.IsNullOrEmpty(item.Value.Integrity) ? item.Value.Integrity : "",
                            !String.IsNullOrEmpty(item.Value.CommandLine) ? item.Value.CommandLine.Replace("\"", "\"\"") : "",
                            !String.IsNullOrEmpty(item.Value.ExistingRegistryPath) ? item.Value.ExistingRegistryPath : "",
                            !String.IsNullOrEmpty(item.Value.ExistingRegistryDescription) ? item.Value.ExistingRegistryDescription.Replace("\"", "\"\"") : "",
                            !String.IsNullOrEmpty(item.Value.ExistingRegistryCOM) ? item.Value.ExistingRegistryCOM.Replace("\"", "\"\"") : ""
                        )
                    );
                }
            }
        }
    }
}
