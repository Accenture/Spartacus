using Spartacus.Spartacus.CommandLine;
using Spartacus.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Spartacus.Modes.PROXY.PrototypeDatabaseGeneration;
using static Spartacus.Utils.PEFileExports;

namespace Spartacus.Modes.PROXY
{
    class ExportsGeneration
    {
        protected Helper Helper = new();

        protected List<FunctionPrototype> ExistingFunctionPrototypes = new();

        public void Run()
        {
            ExistingFunctionPrototypes = Helper.LoadPrototypes(RuntimeData.PrototypesFile);

            Logger.Info("Listing exports for " + RuntimeData.BatchDLLFiles.Count + " file(s)");

            foreach (string dllFile in RuntimeData.BatchDLLFiles)
            {
                ListExports(dllFile);
            }
        }

        protected void ListExports(string dllFile)
        {
            List<FileExport> exports = Helper.GetExportFunctions(dllFile);

            Logger.Info("", true, false);
            Logger.Info($"File: {dllFile}", true, false);
            Logger.Info($"Exports: {exports.Count}", true, false);
            Logger.Info("", true, false);

            Logger.Info("ordinal\tprototype\tname", true, false);
            Logger.Info("", true, false);
            foreach (FileExport export in exports)
            {
                string hasPrototype = ExistingFunctionPrototypes.Count > 0
                    ? ExistingFunctionPrototypes.Where(x => x.name == export.Name).ToList().Count > 0 ? "Yes" : "No"
                    : "N/A";

                string forwarded = export.Forward.Length > 0
                    ? $" (forwarded to {export.Forward})"
                    : "";

                Logger.Info($"{export.Ordinal}\t{hasPrototype}\t\t{export.Name}{forwarded}", true, false);
            }


            Logger.Info("", true, false);
        }
    }
}
