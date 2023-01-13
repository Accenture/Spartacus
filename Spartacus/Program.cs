using Spartacus.ProcMon;
using Spartacus.Spartacus;
using Spartacus.Spartacus.CommandLine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Spartacus
{
    class Program
    {
        static void Main(string[] args)
        {
            string appVersion = String.Format("{0}.{1}.{2}", Assembly.GetExecutingAssembly().GetName().Version.Major.ToString(), Assembly.GetExecutingAssembly().GetName().Version.Minor.ToString(), Assembly.GetExecutingAssembly().GetName().Version.Build.ToString());
            if (args.Length == 0)
            {
                string help = 
$@"Spartacus v{appVersion} [ Accenture Security ]
- For more information visit https://github.com/Accenture/Spartacus

Usage: Spartacus.exe [options]

--pml                   Location (file) to store the ProcMon event log file. If the file exists,
                        it will be overwritten. When used with --existing-log it will indicate
                        the event log file to read from and will not be overwritten.
--pmc                   Define a custom ProcMon (PMC) file to use. This file will not be modified
                        and will be used as is.
--csv                   Location (file) to store the CSV output of the execution.
                        This file will include only the DLLs that were marked as NAME_NOT_FOUND,
                        PATH_NOT_FOUND, and were in user-writable locations (it excludes anything
                        in the Windows and Program Files directories)
--exe                   Define process names (comma separated) that you want to track, helpful
                        when you are interested only in a specific process.
--exports               Location (folder) in which all the proxy DLL files will be saved.
                        Proxy DLL files will only be generated if this argument is used.
--procmon               Location (file) of the SysInternals Process Monitor procmon.exe or procmon64.exe
--proxy-dll-template    Define a DLL template to use for generating the proxy DLL files. Only
                        relevant when --exports is used. All #pragma exports are inserted by
                        replacing the %_PRAGMA_COMMENTS_% string, so make sure your template
                        includes that string in the relevant location.
--existing-log          Switch to indicate that Spartacus should process an existing ProcMon event
                        log file (PML). To indicate the event log file use --pml, useful when you
                        have been running ProcMon for hours or used it in Boot Logging.
--all                   By default any DLLs in the Windows or Program Files directories will be skipped.
                        Use this to include those directories in the output.
--detect                Try to identify DLLs that are proxying calls (like 'DLL Hijacking in progress').
                        This isn't a feature to be relied upon, it's there to get the low hanging fruit.
--verbose               Enable verbose output.
--debug                 Enable debug output.

Examples:

Collect all events and save them into C:\Data\logs.pml. All vulnerable DLLs will be saved as C:\Data\VulnerableDLLFiles.csv and all proxy DLLs in C:\Data\DLLExports.

    --procmon C:\SysInternals\Procmon.exe --pml C:\Data\logs.pml --csv C:\Data\VulnerableDLLFiles.csv --exports C:\Data\DLLExports --verbose

Collect events only for Teams.exe and OneDrive.exe.

    --procmon C:\SysInternals\Procmon.exe --pml C:\Data\logs.pml --csv C:\Data\VulnerableDLLFiles.csv --exports C:\Data\DLLExports --verbose --exe ""Teams.exe,OneDrive.exe""

Collect events only for Teams.exe and OneDrive.exe, and use a custom proxy DLL template at C:\Data\myProxySkeleton.cpp.
        
    --procmon C:\SysInternals\Procmon.exe --pml C:\Data\logs.pml --csv C:\Data\VulnerableDLLFiles.csv --exports C:\Data\DLLExports --verbose --exe ""Teams.exe,OneDrive.exe"" --proxy-dll-template C:\Data\myProxySkeleton.cpp

Collect events only for Teams.exe and OneDrive.exe, but don't generate proxy DLLs.

    --procmon C:\SysInternals\Procmon.exe --pml C:\Data\logs.pml --csv C:\Data\VulnerableDLLFiles.csv --verbose --exe ""Teams.exe,OneDrive.exe""

Parse an existing PML event log output, save output to CSV, and generate proxy DLLs.

    --existing-log --pml C:\MyData\SomeBackup.pml --csv C:\Data\VulnerableDLLFiles.csv --exports C:\Data\DLLExports

Run in monitoring mode and try to detect any applications that is proxying DLL calls.

    --detect
";
                Logger.Info(help, true, false);

#if DEBUG
                Console.ReadLine();
#endif
                return;
            }

            
            Logger.Info($"Spartacus v{appVersion}");

            try
            {
                // This will parse everything into RuntimeData.*
                CommandLineParser cmdParser = new CommandLineParser(args);
            } catch (Exception e) {
                Logger.Error(e.Message);
#if DEBUG
                Console.ReadLine();
#endif
                return;
            }

            try
            {
                if (RuntimeData.DetectProxyingDLLs)
                {
                    Logger.Info("Starting DLL Proxying detection");
                    Logger.Info("", true, false);
                    Logger.Info("This feature is not to be relied upon - I just thought it'd be cool to have.", true, false);
                    Logger.Info("The way it works is by checking if a process has 2 or more DLLs loaded that share the same name but different location.", true, false);
                    Logger.Info("For instance 'version.dll' within the application's directory and C:\\Windows\\System32.", true, false);
                    Logger.Info("", true, false);
                    Logger.Info("There is no progress indicator - when a DLL is found it will be displayed here - hit CTRL-C to exit.");

                    Detect detector = new Detect();
                    detector.Run();
                }
                else if (RuntimeData.GenerateProxy)
                {
                    Logger.Info("Starting proxy DLL generation");
                    ProxyDLLGenerator generator = new ProxyDLLGenerator();
                    generator.Run();
                }
                else
                {
                    Manager manager = new Manager();

                    if (!RuntimeData.ProcessExistingLog)
                    {
                        Logger.Verbose("Making sure there are no ProcessMonitor instances...");
                        manager.TerminateProcessMonitor();

                        if (RuntimeData.ProcMonLogFile != "" && File.Exists(RuntimeData.ProcMonLogFile))
                        {
                            Logger.Verbose("Deleting previous log file: " + RuntimeData.ProcMonLogFile);
                            File.Delete(RuntimeData.ProcMonLogFile);
                        }

                        Logger.Info("Getting PMC file...");
                        string pmcFile = manager.GetPMCFile();

                        Logger.Info("Executing ProcessMonitor...");
                        manager.StartProcessMonitor();

                        Logger.Info("Process Monitor has started...");

                        Logger.Warning("Press ENTER when you want to terminate Process Monitor and parse its output...", false, true);
                        Console.ReadLine();

                        Logger.Info("Terminating Process Monitor...");
                        manager.TerminateProcessMonitor();
                    }

                    Logger.Info("Reading events file...");
                    ProcMonPML log = new ProcMonPML(RuntimeData.ProcMonLogFile);

                    Logger.Info("Found " + String.Format("{0:N0}", log.TotalEvents()) + " events...");

                    EventProcessor processor = new EventProcessor(log);
                    processor.Run();

                    Logger.Info("CSV Output stored in: " + RuntimeData.CsvOutputFile);
                    if (RuntimeData.ExportsOutputDirectory != "")
                    {
                        Logger.Info("Proxy DLLs stored in: " + RuntimeData.ExportsOutputDirectory);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
#if DEBUG
                Console.ReadLine();
#endif
                return;
            }

            Logger.Success("All done");

#if DEBUG
            Console.ReadLine();
#endif
        }
    }
}
