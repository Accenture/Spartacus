using Spartacus.ProcMon;
using Spartacus.Spartacus.CommandLine;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spartacus.Spartacus
{
    class Manager
    {
        public string GetPMCFile()
        {
            string pmcFile = Path.GetTempPath() + Guid.NewGuid().ToString() + ".pmc";
            if (RuntimeData.ProcMonConfigFile != "")
            {
                // We need to see if we must inject the backing file into the passed configuration file.
                if (RuntimeData.InjectBackingFileIntoConfig)
                {
                    Logger.Info("Injecting --pml location into the --pmc file, as the latter has no backing file set.");
                    ProcMonPMC pmc = new ProcMonPMC(RuntimeData.ProcMonConfigFile);
                    RuntimeData.ProcMonConfigFile = pmcFile;
                    Logger.Info("New ProcMon configuration file will be: " + RuntimeData.ProcMonConfigFile);
                    pmc.GetConfiguration().Logfile = RuntimeData.ProcMonLogFile;
                    pmc.GetConfiguration().Save(pmcFile);
                }
                return RuntimeData.ProcMonConfigFile;
            }
            RuntimeData.ProcMonConfigFile = pmcFile;
            Logger.Verbose("ProcMon configuration file will be: " + RuntimeData.ProcMonConfigFile);

            // Otherwise we have to create our own here.
            ProcMonConfig config = new ProcMonConfig();
            
            config.AddColumn(ProcMonConstants.FilterRuleColumn.TIME_OF_DAY, 100);
            config.AddColumn(ProcMonConstants.FilterRuleColumn.PROCESS_NAME, 100);
            config.AddColumn(ProcMonConstants.FilterRuleColumn.PID, 100);
            config.AddColumn(ProcMonConstants.FilterRuleColumn.OPERATION, 100);
            config.AddColumn(ProcMonConstants.FilterRuleColumn.PATH, 100);
            config.AddColumn(ProcMonConstants.FilterRuleColumn.RESULT, 100);
            config.AddColumn(ProcMonConstants.FilterRuleColumn.DETAIL, 100);
            config.AddColumn(ProcMonConstants.FilterRuleColumn.ARCHITECTURE, 100);

            // We don't add the Windows/Program Files directory because otherwise we won't be able to find
            // the DLL that was actually loaded.
            config.AddFilter(ProcMonConstants.FilterRuleColumn.OPERATION, ProcMonConstants.FilterRuleRelation.IS, ProcMonConstants.FilterRuleAction.INCLUDE, "CreateFile");
            config.AddFilter(ProcMonConstants.FilterRuleColumn.PATH, ProcMonConstants.FilterRuleRelation.ENDS_WITH, ProcMonConstants.FilterRuleAction.INCLUDE, ".dll");
            //config.AddFilter(ProcMonConstants.FilterRuleColumn.PATH, ProcMonConstants.FilterRuleRelation.BEGINS_WITH, ProcMonConstants.FilterRuleAction.EXCLUDE, "C:\\Windows");
            //config.AddFilter(ProcMonConstants.FilterRuleColumn.PATH, ProcMonConstants.FilterRuleRelation.BEGINS_WITH, ProcMonConstants.FilterRuleAction.EXCLUDE, "C:\\Program Files");
            config.AddFilter(ProcMonConstants.FilterRuleColumn.PROCESS_NAME, ProcMonConstants.FilterRuleRelation.IS, ProcMonConstants.FilterRuleAction.EXCLUDE, "procmon.exe");
            config.AddFilter(ProcMonConstants.FilterRuleColumn.PROCESS_NAME, ProcMonConstants.FilterRuleRelation.IS, ProcMonConstants.FilterRuleAction.EXCLUDE, "procmon64.exe");

            foreach (string executable in RuntimeData.TrackExecutables)
            {
                config.AddFilter(
                    ProcMonConstants.FilterRuleColumn.PROCESS_NAME,
                    ProcMonConstants.FilterRuleRelation.IS,
                    ProcMonConstants.FilterRuleAction.INCLUDE,
                    executable
                );
            }

            config.Autoscroll = 0;
            config.DestructiveFilter = 1;
            config.Logfile = RuntimeData.ProcMonLogFile;

            config.Save(RuntimeData.ProcMonConfigFile);

            return RuntimeData.ProcMonConfigFile;
        }

        public void StartProcessMonitor()
        {
            string procMonArguments = $"/AcceptEula /Quiet /Minimized /LoadConfig \"{RuntimeData.ProcMonConfigFile}\"";

            Process process = Process.Start(RuntimeData.ProcMonExecutable, procMonArguments);
            process.WaitForInputIdle(5000);
        }

        public void TerminateProcessMonitor()
        {
            Process process = Process.Start(RuntimeData.ProcMonExecutable, "/AcceptEula /Terminate");
            process.WaitForExit();
        }
    }
}
