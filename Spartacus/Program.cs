using Spartacus.ProcMon;
using Spartacus.Spartacus;
using Spartacus.Spartacus.CommandLine;
using Spartacus.Utils;
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
            
            Logger.Info($@"Spartacus v{appVersion} [ Accenture Security ]", true, false);
            Logger.Info($@"- For more information visit https://github.com/Accenture/Spartacus", true, false);
            Logger.Info("", true, false);

            Helper helper = new();

            // Parse passed arguments into RuntimeData.*
            try
            {
                if (args.Length == 0)
                {
                    helper.ShowHelp();
#if DEBUG
                    Console.ReadLine();
#endif
                    return;
                }

                Logger.Verbose("Loading command line arguments...");
                CommandLineParser command = new(args);

                if (RuntimeData.isHelp)
                {
                    helper.ShowHelp();
#if DEBUG
                    Console.ReadLine();
#endif
                    return;
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
