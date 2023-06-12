using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Spartacus.Modes.DETECT
{
    class ModeDetect : ModeBase
    {
        private struct ProcessInfoStruct
        {
            public Process process;
            public List<string> DLLs;
        }

        private Dictionary<int, List<string>> AlreadyDetected = new ();

        private List<string> OSPaths = new();

        public override void Run()
        {
            ShowInfo();

            this.OSPaths = Helper.GetOSPaths();

            do
            {
                try
                {
                    List<Task<ProcessInfoStruct>> allTasks = new List<Task<ProcessInfoStruct>>();
                    allTasks.Clear();
                    
                    // Create one task for each process we need to get the modules (DLLs) for. This drops execution from 7 seconds down to 2.
                    foreach (Process process in Process.GetProcesses())
                    {
                        Task<ProcessInfoStruct> task = new Task<ProcessInfoStruct>(() => GetProcessInfo(process));
                        task.Start();
                        allTasks.Add(task);
                    }

                    // Now that all tasks are doing their thing, wait until they are all completed.
                    Task.WaitAll(allTasks.ToArray());

                    // At this point we have all the results we need, do just process it.
                    ProcessResults(allTasks);
                }
                catch (Exception e)
                {
                    // Sometimes we'll try to access a process that has already exited.
                    Logger.Error(e.Message + " - nothing to worrry about, Spartacus keeps running");
                }

                // Adding a sleep here to give the CPU some breathing room.
                Thread.Sleep(1000);
            } while (true);
        }

        protected void ShowInfo()
        {
            Logger.Info("Starting DLL Proxying detection");
            Logger.Info("", true, false);
            Logger.Info("This feature is not to be blindly relied upon as it may return a lot of false positives.", true, false);
            Logger.Info("The way it works is by checking if a process has 2 or more DLLs loaded that share the same name but different location.", true, false);
            Logger.Info("For instance 'version.dll' within the application's directory and C:\\Windows\\System32.", true, false);
            Logger.Info("", true, false);
            Logger.Info("There is no progress indicator - when a DLL is found it will be displayed here - hit CTRL-C to exit.");
        }

        public override void SanitiseAndValidateRuntimeData()
        {
            // Nothing to do here.
        }

        private ProcessInfoStruct GetProcessInfo(Process process)
        {
            ProcessInfoStruct info = new ProcessInfoStruct();
            // Saving it here as it's a pain to try and get from the result of an async task.
            info.process = process;
            info.DLLs = new List<string>();

            try
            {
                // Put this in a try-catch as the .Modules will return AccessDenied if don't have the right privileges.
                foreach (ProcessModule module in process.Modules)
                {
                    info.DLLs.Add(module.FileName);
                }
            }
            catch (Exception e)
            {
                // Clear.
                info.DLLs = new List<string>();
            }

            return info;
        }

        private void ProcessResults(List<Task<ProcessInfoStruct>> tasks)
        {
            foreach (Task<ProcessInfoStruct> task in tasks)
            {
                ProcessInfoStruct result = task.Result;
                if (result.DLLs.Count() == 0)
                {
                    // No DLLs for this process, probably cause we got denied when tried to access it.
                    continue;
                }

                List<string> findings = FindProxyingDLLs(result.DLLs);
                if (findings.Count() == 0)
                {
                    // No duplicate DLLs found.
                    continue;
                }

                foreach (string dll in findings)
                {
                    // This will also add the DLL into the "known DLLs" mapping.
                    if (!IsAlreadyDetected(result.process, dll))
                    {
                        Logger.Success("Potential proxying DLL: " + dll);
                        Logger.Warning("Loaded by [" + result.process.Id + "] " + result.process.MainModule.FileName);
                    }
                }
            }
        }

        private bool IsAlreadyDetected(Process process, string dll)
        {
            dll = dll.ToLower();
            if (AlreadyDetected.ContainsKey(process.Id))
            {
                if (AlreadyDetected[process.Id].Contains(dll))
                {
                    return true;
                }
            }

            // It doesn't exist.
            if (!AlreadyDetected.ContainsKey(process.Id))
            {
                AlreadyDetected.Add(process.Id, new List<string>());
            }
            AlreadyDetected[process.Id].Add(dll);

            return false;
        }

        private List<string> FindProxyingDLLs(List<string> DLLs)
        {
            Dictionary<string, string> dllNameFileMapping = new Dictionary<string, string>();

            List<string> findings = new List<string>();
            foreach (string dll in DLLs)
            {
                string filename = Path.GetFileName(dll).ToLower();
                if (!dllNameFileMapping.ContainsKey(filename))
                {
                    // This is the first time we see this DLL in this process, so it can't be a duplicate one.
                    dllNameFileMapping.Add(filename, dll);
                    continue;
                }

                // If we got here, it means we have found a DLL with the same name within the same process.
                string previousFile = dllNameFileMapping[filename];
                bool previousFileInOSPath = IsFileInOSDirectory(previousFile);
                bool currentFileInOSPath = IsFileInOSDirectory(dll);

                if (previousFileInOSPath && currentFileInOSPath)
                {
                    // Both files are in an OS path, ignore.
                }
                else if (previousFileInOSPath && !currentFileInOSPath)
                {
                    // The previous instance is in an OS path while the current one isn't.
                    findings.Add(dll);
                }
                else if (!previousFileInOSPath && currentFileInOSPath)
                {
                    // The current instance is in an OS path while the previous one isn't.
                    findings.Add(previousFile);
                }
                else if ((!previousFileInOSPath && !currentFileInOSPath) && (previousFile.ToLower() != dll.ToLower()))
                {
                    // Both files are outside of OS paths, report both of them.
                    findings.Add(previousFile);
                    findings.Add(dll);
                }
            }

            return findings;
        }

        private bool IsFileInOSDirectory(string path)
        {
            foreach (string osPath in OSPaths)
            {
                if (path.ToLower().StartsWith(osPath))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
