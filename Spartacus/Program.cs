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
            
            Logger.Info($@"Spartacus v{appVersion} [ Accenture Security ]");
            Logger.Info($@"- For more information visit https://github.com/Accenture/Spartacus");
            Logger.Info("");

            if (args.Length == 0)
            {
                // TODO - Show help.
#if DEBUG
                Console.ReadLine();
#endif
                return;
            }

            // Parse passed arguments into RuntimeData.*
            try
            {
                Logger.Verbose("Loading command line arguments...");
                CommandLineParser command = new(args);
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
#if DEBUG
                Console.ReadLine();
#endif
                return;
            }

            // Now that we have a loaded mode object, execute it.
            try
            {
                Logger.Verbose($"Running {RuntimeData.Mode} mode...");
                RuntimeData.ModeObject.Run();
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
