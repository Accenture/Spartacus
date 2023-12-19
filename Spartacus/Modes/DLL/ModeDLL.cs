using Spartacus.Modes.PROXY;
using Spartacus.ProcMon;
using Spartacus.Properties;
using Spartacus.Spartacus;
using Spartacus.Spartacus.CommandLine;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static Spartacus.ProcMon.ProcMonConstants;

namespace Spartacus.Modes.DLL
{
    class ModeDLL : ModeBase
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

            Logger.Verbose("Found " + String.Format("{0:N0}", log.TotalEvents()) + " events...");

            // Find all events of interest, like DLLs that weren't loaded etc.
            Stopwatch watch = Stopwatch.StartNew();
            Dictionary<string, PMLEvent> events = FindInterestingEvents(log);
            watch.Stop();
            Logger.Debug(String.Format("FindEvents() took {0:N0}ms", watch.ElapsedMilliseconds));

            // Find the actual DLLs that were loaded.
            watch.Restart();
            events = FindLoadedEvents(log, events);
            watch.Stop();
            Logger.Debug(String.Format("FindLoadedEvents() took {0:N0}ms", watch.ElapsedMilliseconds));

            // Save output to CSV.
            do
            {
                try
                {
                    ExportToCSV(events);
                    Logger.Info("CSV output saved to: " + RuntimeData.CSVFile);
                    break;  // Saved successfully.
                } catch (Exception e)
                {
                    Logger.Error(e.Message);
                    if (RuntimeData.Debug)
                    {
                        Logger.Error(e.StackTrace);
                    }
                    Logger.Warning("There was an error saving the output. In order to avoid losing the processed data");
                    Logger.Warning("we're going to give it another go. When you resolve the error described above");
                    Logger.Warning("hit ENTER and another attempt at saving the output will be made.", false, true);
                    Console.ReadLine();
                    Logger.Warning("Trying to save file again...");
                }
            } while (true);

            // Create solutions for identified DLLs.
            if (!String.IsNullOrEmpty(RuntimeData.Solution) && Directory.Exists(RuntimeData.Solution))
            {
                CreateSolutionsForDLLs(events);
            }
        }

        protected void CreateSolutionsForDLLs(Dictionary<string, PMLEvent> events)
        {
            // First we collect which files we need to proxy.
            Logger.Verbose("Identifying files to generate solutions for...");
            Dictionary<string, string> filesToProxy = new();
            foreach (KeyValuePair<string, PMLEvent> e in events)
            {
                string dllFilename = Path.GetFileName(e.Value.Path).ToLower();
                if (String.IsNullOrEmpty(dllFilename) || filesToProxy.ContainsKey(dllFilename))
                {
                    continue;
                }

                Logger.Debug("File to proxy: " + e.Value.FoundPath);
                filesToProxy.Add(dllFilename, e.Value.FoundPath);
            }

            // Now we create the proxies.
            ProxyGeneration proxyMode = new();
            foreach (KeyValuePair<string, string> file in filesToProxy.OrderBy(x => x.Key))
            {
                Logger.Info("Processing " + file.Key, false, true);
                string solution = Path.Combine(RuntimeData.Solution, Path.GetFileNameWithoutExtension(file.Value));
                string dllFile = Helper.LookForFileIfNeeded(file.Value);

                if (String.IsNullOrEmpty(file.Value) || String.IsNullOrEmpty(dllFile) || !File.Exists(dllFile))
                {
                    try
                    {
                        File.Create(Path.Combine(solution, file.Key + "-file-not-found")).Dispose();
                    }
                    catch (Exception e)
                    {
                        Logger.Warning(" - error creating", false, false);
                    }
                    Logger.Warning(" - No DLL Found", true, false);
                    continue;
                }
                else
                {
                    Logger.Info(" - Found", true, false);
                }

                if (!proxyMode.ProcessSingleDLL(dllFile, solution))
                {
                    Logger.Error("Could not generate proxy DLL for: " + dllFile);
                }
            }
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
            RuntimeData.PMCFile = procMon.CreateConfigForDLL(RuntimeData.PMCFile, RuntimeData.InjectBackingFileIntoConfig, RuntimeData.PMLFile);

            Logger.Info("Starting ProcessMonitor...");
            procMon.Start(RuntimeData.PMCFile);

            Logger.Verbose("Process Monitor has started...");

            Logger.Warning("Press ENTER when you want to terminate Process Monitor and parse its output...", false, true);
            Console.ReadLine();

            Logger.Info("Terminating Process Monitor...");
            procMon.Terminate();
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

            // Solution folder.
            if (String.IsNullOrEmpty(RuntimeData.Solution))
            {
                Logger.Debug("--solution is missing, will skip DLL proxy generation");
            }
            else if (Directory.Exists(RuntimeData.Solution))
            {
                Logger.Debug("--solution directory already exists");
            }
            else
            {
                Logger.Debug("--solution directory does not exist - creating now");
                if (!Helper.CreateTargetDirectory(RuntimeData.Solution))
                {
                    throw new Exception("Could not create --solution directory: " + RuntimeData.Solution);
                }
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
                throw new Exception("--pml does not exist: " +  RuntimeData.PMLFile);
            }
            Logger.Debug("--pml is " + RuntimeData.PMLFile);
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

        protected Dictionary<string, PMLEvent> FindInterestingEvents(ProcMonPML log)
        {
            UInt32 counter = 0;
            UInt32 steps = log.TotalEvents() / 10;
            if (steps == 0)
            {
                steps = 1;
            }

            // Get the OS paths (program files, windows, etc) so that we can filter out files in those folders
            // as they are usually non-writable.
            List<string> privilegedPaths = Helper.GetOSPaths();

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

                // We want a "CreateFile" that is either a "NAME NOT FOUND" or a "PATH NOT FOUND".
                if (e.Operation != EventFileSystemOperation.CreateFile)
                {
                    continue;
                }
                else if (e.Result != EventResult.NAME_NOT_FOUND && e.Result != EventResult.PATH_NOT_FOUND)
                {
                    continue;
                }
                else if (e.EventClass != EventClassType.File_System)
                {
                    continue;
                }

                // Ignore extensions that aren't *.dll
                string p = e.Path.ToLower();
                if (!p.EndsWith(".dll"))
                {
                    continue;
                }
                else if (!RuntimeData.All)
                {
                    // Exclude any DLLs that are in directories that are writable only by privileged users.
                    // Check if the path belongs to an OS path, like windows, program files, etc.
                    bool isPrivileged = false;
                    foreach (string path in privilegedPaths)
                    {
                        if (p.StartsWith(path))
                        {
                            isPrivileged = true;
                            break;
                        }
                    }
                    if (isPrivileged)
                    {
                        continue;
                    }
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

        protected Dictionary<string, PMLEvent> FindLoadedEvents(ProcMonPML log, Dictionary<string, PMLEvent> events)
        {
            // Now that we have the missing paths, we extract only the filenames of the DLLs.
            // This is done so that further down we can try and find the DLLs that _were_ loaded from different paths.
            Logger.Verbose("Extracting DLL filenames from paths...");
            // Key will be the filename, value will be the path.
            List<string> missingDLLs = new();
            foreach (KeyValuePair<string, PMLEvent> item in events)
            {
                string name = Path.GetFileName(item.Key).ToLower();
                if (!missingDLLs.Contains(name))
                {
                    missingDLLs.Add(name);
                }
            }
            Logger.Verbose("Found " + String.Format("{0:N0}", missingDLLs.Count()) + " unique DLLs...");

            if (missingDLLs.Count == 0)
            {
                return events;
            }

            UInt32 counter = 0;
            UInt32 steps = log.TotalEvents() / 10;
            if (steps == 0)
            {
                steps = 1;
            }
            log.Rewind();

            Logger.Info("Trying to identify which DLLs were actually loaded...", false, true);

            // Now try to find the actual DLL that was loaded. For example if 'version.dll' was missing, identify
            // the location it was eventually loaded from.
            do
            {
                if (++counter % steps == 0)
                {
                    Logger.Info(".", false, false);
                }

                PMLEvent e = log.GetNextEvent().GetValueOrDefault();
                if (!e.Loaded)
                {
                    break;
                }

                // Now we are looking for "CreateFile" SUCCESS events.
                string p = e.Path.ToLower();
                if (!p.EndsWith(".dll"))
                {
                    continue;
                }
                else if (e.Result != EventResult.SUCCESS)
                {
                    continue;
                }
                else if (e.Operation != EventFileSystemOperation.CreateFile)
                {
                    continue;
                }

                // If we are here it means we have found a DLL that was actually loaded. Extract its name.
                string name = Path.GetFileName(p).ToLower();
                if (name == "")
                {
                    continue;
                }
                else if (!missingDLLs.Contains(name))
                {
                    // We found a SUCCESS DLL but it's not one that is vulnerable.
                    continue;
                }

                // Find all events of interest (NAME/PATH NOT FOUND) that use the same DLL.
                List<string> keys = events
                    .Where(ve => Path.GetFileName(ve.Key).ToLower() == name && ve.Value.FoundPath == "")
                    .Select(ve => ve.Key)
                    .ToList();

                foreach (string key in keys)
                {
                    PMLEvent Event = events[key];
                    Event.FoundPath = e.Path;
                    events[key] = Event;
                }

                missingDLLs.Remove(name);
                if (missingDLLs.Count == 0)
                {
                    // Abort if we have no other DLLs to look for.
                    break;
                }
            } while (true);
            Logger.Info("", true, false);

            Logger.Debug(String.Format("Identified final events: {0}", events.Count()));

            return events;
        }

        protected void ExportToCSV(Dictionary<string, PMLEvent> events)
        {
            Logger.Info("Saving to CSV...");
            using (StreamWriter stream = File.CreateText(RuntimeData.CSVFile))
            {
                stream.WriteLine(string.Format("Process,Image Path,Missing DLL,Found DLL,Integrity,Command Line"));
                foreach (KeyValuePair<string, PMLEvent> item in events)
                {
                    stream.WriteLine(
                        string.Format(
                            "\"{0}\",\"{1}\",\"{2}\",\"{3}\",\"{4}\",\"{5}\"",
                            item.Value.Process.ProcessName,
                            item.Value.Process.ImagePath,
                            item.Value.Path,
                            item.Value.FoundPath,
                            item.Value.Process.Integrity,
                            item.Value.Process.CommandLine.Replace("\"", "\"\""))
                        );
                }
            }
        }
    }
}
