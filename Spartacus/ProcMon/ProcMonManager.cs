using Spartacus.Spartacus.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Spartacus.ProcMon.ProcMonConstants;

namespace Spartacus.ProcMon
{
    class ProcMonManager
    {
        private readonly string ExePath = "";

        public ProcMonManager(string exePath)
        {
            this.ExePath = exePath;
        }

        public string CreateConfigFile(string saveAs, string pmcFile, bool injectPMCFile, string pmlFile, List<PMCFilter> filters)
        {
            //string newPMCFile = Path.GetTempPath() + Guid.NewGuid().ToString() + ".pmc";
            string newPMCFile = saveAs;
            Logger.Verbose($"ProcMon configuration file will be: {newPMCFile}");
            if (!String.IsNullOrEmpty(pmcFile))
            {
                // An existing config file has been passed. Check if we need to inject the PML file as well.
                if (injectPMCFile)
                {
                    // To avoid overwriting the passed pmcFile, we make a copy and save under a new name.
                    Logger.Verbose("Injecting --pml location into the --pmc file, as the latter has no backing file set.");
                    ProcMonPMC pmc = new(pmcFile);
                    pmc.GetConfiguration().Logfile = pmlFile;
                    pmc.GetConfiguration().Save(pmcFile);
                }
                // Return early, no need to generate a fresh PMC file.
                return newPMCFile;
            }

            // Let's create a new PMC file.
            ProcMonConfig config = new();

            // Add columns.
            config.AddColumn(ProcMonConstants.FilterRuleColumn.TIME_OF_DAY, 100);
            config.AddColumn(ProcMonConstants.FilterRuleColumn.PROCESS_NAME, 100);
            config.AddColumn(ProcMonConstants.FilterRuleColumn.PID, 100);
            config.AddColumn(ProcMonConstants.FilterRuleColumn.OPERATION, 100);
            config.AddColumn(ProcMonConstants.FilterRuleColumn.PATH, 100);
            config.AddColumn(ProcMonConstants.FilterRuleColumn.RESULT, 100);
            config.AddColumn(ProcMonConstants.FilterRuleColumn.DETAIL, 100);
            config.AddColumn(ProcMonConstants.FilterRuleColumn.ARCHITECTURE, 100);

            // Add filters.
            foreach (PMCFilter filter in filters)
            {
                config.AddFilter(filter.Column, filter.Relation, filter.Action, filter.Value);
            }

            config.Autoscroll = 0;
            config.DestructiveFilter = 1;
            config.Logfile = pmlFile;

            // Save.
            config.Save(newPMCFile);

            return newPMCFile;
        }

        public string CreateConfigForDLL(string pmcFile, bool injectPMCFile, string pmlFile)
        {
            string newPMCFile = Path.GetTempPath() + Guid.NewGuid().ToString() + ".pmc";

            List<PMCFilter> filters = new()
            {
                new PMCFilter() { Column = FilterRuleColumn.OPERATION, Relation = FilterRuleRelation.IS, Action = FilterRuleAction.INCLUDE, Value = "CreateFile" },
                new PMCFilter() { Column = FilterRuleColumn.PATH, Relation = FilterRuleRelation.ENDS_WITH, Action = FilterRuleAction.INCLUDE, Value = ".dll" },
                new PMCFilter() { Column = FilterRuleColumn.PROCESS_NAME, Relation = FilterRuleRelation.IS, Action = FilterRuleAction.EXCLUDE, Value = "procmon.exe" },
                new PMCFilter() { Column = FilterRuleColumn.PROCESS_NAME, Relation = FilterRuleRelation.IS, Action = FilterRuleAction.EXCLUDE, Value = "procmon64.exe" },
            };

            return CreateConfigFile(newPMCFile, pmcFile, injectPMCFile, pmlFile, filters);
        }

        public string CreateConfigForCOM(string pmcFile, bool injectPMCFile, string pmlFile)
        {
            string newPMCFile = Path.GetTempPath() + Guid.NewGuid().ToString() + ".pmc";

            List<PMCFilter> filters = new()
            {
                new PMCFilter() { Column = FilterRuleColumn.OPERATION, Relation = FilterRuleRelation.IS, Action = FilterRuleAction.INCLUDE, Value = "RegOpenKey" },
                new PMCFilter() { Column = FilterRuleColumn.PATH, Relation = FilterRuleRelation.ENDS_WITH, Action = FilterRuleAction.INCLUDE, Value = "InprocServer32" },
                new PMCFilter() { Column = FilterRuleColumn.PROCESS_NAME, Relation = FilterRuleRelation.IS, Action = FilterRuleAction.EXCLUDE, Value = "procmon.exe" },
                new PMCFilter() { Column = FilterRuleColumn.PROCESS_NAME, Relation = FilterRuleRelation.IS, Action = FilterRuleAction.EXCLUDE, Value = "procmon64.exe" },
            };

            return CreateConfigFile(newPMCFile, pmcFile, injectPMCFile, pmlFile, filters);

        }

        public void Start(string configFile)
        {
            List<string> arguments = new List<string>
            {
                "/AcceptEula",
                "/Quiet",
                "/Minimized",
                "/LoadConfig",
                $"\"{configFile}\"",
            };

            Process process = Process.Start(ExePath, String.Join(" ", arguments));
            process.WaitForInputIdle(5000);
        }

        public void Terminate()
        {
            List<string> arguments = new List<string>
            {
                "/AcceptEula",
                "/Terminate"
            };

            Process process = Process.Start(ExePath, String.Join(" ", arguments));
            process.WaitForExit();
        }
    }
}
