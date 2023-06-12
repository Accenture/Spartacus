using Spartacus.ProcMon;
using Spartacus.Properties;
using Spartacus.Spartacus.CommandLine;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Spartacus.ProcMon.ProcMonConstants;
using static Spartacus.Spartacus.PEFileExports;

namespace Spartacus.Spartacus
{
    class EventProcessor
    {
        private readonly ProcMonPML PMLog;

        private Dictionary<string, PMLEvent> EventsOfInterest = new Dictionary<string, PMLEvent>();

        public EventProcessor(ProcMonPML log)
        {
            PMLog = log;
        }

        public void Run()
        {
            // Find all events that indicate that a DLL is vulnerable.
            Stopwatch watch = Stopwatch.StartNew();

            FindEvents();
            
            watch.Stop();
            if (RuntimeData.Debug)
            {
                Logger.Debug(String.Format("FindEvents() took {0:N0}ms", watch.ElapsedMilliseconds));
            }

            // Extract all DLL paths into a list.
            Logger.Verbose("Extract DLL paths from events of interest...");
            List<string> missingDLLs = new List<string>();
            foreach (KeyValuePair<string, PMLEvent> item in EventsOfInterest)
            {
                string name = Path.GetFileName(item.Key).ToLower();
                if (!missingDLLs.Contains(name))
                {
                    missingDLLs.Add(name);
                }
            }
            Logger.Verbose("Found " + String.Format("{0:N0}", missingDLLs.Count()) + " unique DLLs...");

            // Now try to find the actual DLL that was loaded. For example if 'version.dll' was missing, identify
            // the location it was eventually loaded from.
            watch.Restart();

            IdentifySuccessfulEvents(missingDLLs);
            
            watch.Stop();
            if (RuntimeData.Debug)
            {
                Logger.Debug(String.Format("IdentifySuccessfulEvents() took {0:N0}ms", watch.ElapsedMilliseconds));
            }


            if (RuntimeData.ExportsOutputDirectory != "" && Directory.Exists(RuntimeData.ExportsOutputDirectory))
            {
                ExtractExportFunctions();
            }

            try
            {
                SaveEventsOfInterest();
            } catch (Exception e)
            {
                Logger.Error(e.Message);
                Logger.Warning("There was an error saving the output. In order to avoid losing the processed data");
                Logger.Warning("we're going to give it another go. When you resolve the error described above");
                Logger.Warning("hit ENTER and another attempt at saving the output will be made.", false, true);
                Console.ReadLine();
                Logger.Warning("Trying to save file again...");
                SaveEventsOfInterest();
            }
        }

        private string LookForFileIfNeeded(string filePath)
        {
            // When we get a path it may be either x32 or a x64. As Spartacus is x64 we can search in the x32 locations if needed.
            if (File.Exists(filePath))
            {
                return filePath;
            }

            // There should really be a case-insensitive replace.
            if (filePath.StartsWith(Environment.GetFolderPath(Environment.SpecialFolder.System), StringComparison.OrdinalIgnoreCase))
            {   
                return Environment.GetFolderPath(Environment.SpecialFolder.SystemX86) + filePath.Remove(0, Environment.GetFolderPath(Environment.SpecialFolder.System).Length);
            }
            else if (filePath.StartsWith(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), StringComparison.OrdinalIgnoreCase))
            {
                return Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) + filePath.Remove(0, Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles).Length);
            }

            // Otherwise return the original value.
            return filePath;
        }

        private void ExtractExportFunctions()
        {
            Logger.Info("Extracting DLL export functions...");

            PEFileExports ExportLoader = new PEFileExports();

            List<string> alreadyProcessed = new List<string>();

            foreach (KeyValuePair<string, PMLEvent> item in EventsOfInterest)
            {
                if (alreadyProcessed.Contains(Path.GetFileName(item.Value.Path).ToLower()))
                {
                    continue;
                }
                alreadyProcessed.Add(Path.GetFileName(item.Value.Path).ToLower());
                Logger.Info("Processing " + Path.GetFileName(item.Value.Path), false, true);
                string saveAs = Path.Combine(RuntimeData.ExportsOutputDirectory, Path.GetFileName(item.Value.Path) + ".cpp");

                if (item.Value.FoundPath == "")
                {
                    File.Create(saveAs + "-file-not-found").Dispose();
                    Logger.Warning(" - No DLL Found", true, false);
                    continue;
                }

                string actualLocation = LookForFileIfNeeded(item.Value.FoundPath);
                if (!File.Exists(actualLocation))
                {
                    File.Create(saveAs + "-file-not-found").Dispose();
                    Logger.Warning(" - No DLL Found", true, false);
                    continue;
                }

                string actualPathNoExtension = Path.Combine(Path.GetDirectoryName(actualLocation), Path.GetFileNameWithoutExtension(actualLocation));

                List<FileExport> exports = new List<FileExport>();
                try
                {
                    exports = ExportLoader.Extract(actualLocation);
                } catch (Exception e)
                {
                    // Nothing.
                }
                
                if (exports.Count == 0)
                {
                    File.Create(saveAs + "-no-exports-found").Dispose();
                    Logger.Warning(" - No export functions found", true, false);
                    continue;
                }

                List<string> pragma = new List<string>();
                string pragmaTemplate = "#pragma comment(linker,\"/export:{0}={1}.{2},@{3}\")";
                int steps = exports.Count() / 10;
                if (steps == 0)
                {
                    steps = 1;
                }
                int counter = 0;
                foreach (FileExport f in exports)
                {
                    if (++counter % steps == 0)
                    {
                        Logger.Info(".", false, false);
                    }
                    pragma.Add(String.Format(pragmaTemplate, f.Name, actualPathNoExtension.Replace("\\", "\\\\"), f.Name, f.Ordinal));
                }

                string fileContents = RuntimeData.TemplateProxyDLL.Replace("%_PRAGMA_COMMENTS_%", String.Join("\r\n", pragma.ToArray()));
                File.WriteAllText(saveAs, fileContents);

                Logger.Success("OK", true, false);
            }
        }

        private void SaveEventsOfInterest()
        {
            Logger.Info("Saving output...");

            using (StreamWriter stream = File.CreateText(RuntimeData.CsvOutputFile))
            {
                stream.WriteLine(string.Format("Process, Image Path, Missing DLL, Found DLL, Integrity, Command Line"));
                foreach (KeyValuePair<string, PMLEvent> item in EventsOfInterest)
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

        private void IdentifySuccessfulEvents(List<string> MissingDLLs)
        {
            if (MissingDLLs.Count() == 0)
            {
                Logger.Verbose("No DLLs identified - skipping successful event tracking");
                return;
            }

            UInt32 counter = 0;
            UInt32 steps = PMLog.TotalEvents() / 10;
            if (steps == 0)
            {
                steps = 1;
            }

            Logger.Info("Trying to identify which DLLs were actually loaded...", false, true);
            PMLog.Rewind();
            do
            {
                if (++counter % steps == 0)
                {
                    Logger.Info(".", false, false);
                }

                PMLEvent e = PMLog.GetNextEvent().GetValueOrDefault();
                if (!e.Loaded)
                {
                    break;
                }

                // Now we are looking for "CreateFile" SUCCESS events.
                string p = e.Path.ToLower();
                if (!p.EndsWith(".dll".ToLower()))
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
                string name = Path.GetFileName(p);
                if (name == "")
                {
                    continue;
                }
                else if (!MissingDLLs.Contains(name))
                {
                    // We found a SUCCESS DLL but it's not one that is vulnerable.
                    continue;
                }

                // Find all events of interest (NAME/PATH NOT FOUND) that use the same DLL.
                List<string> keys = EventsOfInterest
                    .Where(ve => Path.GetFileName(ve.Key).ToLower() == name && ve.Value.FoundPath == "")
                    .Select(ve => ve.Key)
                    .ToList();

                foreach (string key in keys)
                {
                    PMLEvent Event = EventsOfInterest[key];
                    Event.FoundPath = e.Path;
                    EventsOfInterest[key] = Event;
                }

                MissingDLLs.Remove(name);
                if (MissingDLLs.Count == 0)
                {
                    // Abort if we have no other DLLs to look for.
                    break;
                }
            } while (true);
            Logger.Info("", true, false);
        }

        private void FindEvents()
        {
            UInt32 counter = 0;
            UInt32 steps = PMLog.TotalEvents() / 10;
            if (steps == 0)
            {
                steps = 1;
            }

            Logger.Info("Searching events...", false, true);
            PMLog.Rewind();
            do
            {
                if (++counter % steps == 0)
                {
                    Logger.Info(".", false, false);
                }

                // Get the next event from the stream.
                PMLEvent e = PMLog.GetNextEvent().GetValueOrDefault();
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

                // Exclude any DLLs that are in directories that are writable only by privileged users.
                string p = e.Path.ToLower();
                if (!p.EndsWith(".dll".ToLower()))
                {
                    continue;
                }
                else if (!RuntimeData.IncludeAllDLLs && (p.StartsWith(Environment.ExpandEnvironmentVariables("%ProgramW6432%").ToLower()) || p.StartsWith(Environment.GetEnvironmentVariable("SystemRoot").ToLower())))
                {
                    continue;
                }

                // Don't add duplicates.
                if (EventsOfInterest.ContainsKey(p))
                {
                    continue;
                }

                EventsOfInterest.Add(p, e);
            } while (true);
            Logger.Info("", true, false);
            Logger.Info("Found " + String.Format("{0:N0}", EventsOfInterest.Count()) + " events of interest...");
        }
    }
}
