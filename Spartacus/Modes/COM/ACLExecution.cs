using Microsoft.Win32;
using Spartacus.Spartacus.CommandLine;
using Spartacus.Spartacus.Models;
using Spartacus.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Spartacus.ProcMon.ProcMonConstants;

namespace Spartacus.Modes.COM
{
    class ACLExecution
    {
        protected Helper Helper = new();

        private List<string> LookForKeys = new List<string>()
        {
            "inprocserver32",
            "inprocserver",
            "localserver32",
            "localserver"
        };

        protected enum Result
        {
            MISSING_PATH = 1,
            MODIFY = 2,
            WRITE = 3,
            DELETE = 4,
            OWNERSHIP = 5,
        }

        protected struct ACLStruct
        {
            public string regPath;
            public string filePath;
            public Result result;
        }

        protected List<ACLStruct> Findings = new();

        public void Run()
        {
            Logger.Info("Checking local registry for COM objects...");
            
            Logger.Info("Searching HKEY_CLASSES_ROOT...");
            SearchRegistry(Registry.ClassesRoot);
            Logger.Verbose("Total keys found: " + Findings.Count);

            Logger.Info("Searching HKEY_CURRENT_USER...");
            SearchRegistry(Registry.CurrentUser);
            Logger.Verbose("Total keys found: " + Findings.Count);

            Logger.Info("Searching HKEY_LOCAL_MACHINE...");
            SearchRegistry(Registry.LocalMachine);
            
            Logger.Info("Total keys found: " + Findings.Count);

            // Save output to CSV.
            do
            {
                try
                {
                    ExportToCSV(Findings);
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

        protected void ExportToCSV(List<ACLStruct> findings)
        {
            Logger.Info("Saving to CSV...");
            using (StreamWriter stream = File.CreateText(RuntimeData.CSVFile))
            {
                stream.WriteLine(string.Format("Registry Path,COM File Path,Result"));
                foreach (ACLStruct item in findings)
                {
                    stream.WriteLine(
                        string.Format(
                            "\"{0}\",\"{1}\",\"{2}\"",
                            item.regPath,
                            item.filePath.Replace("\"", "\"\""),
                            item.result
                        )
                    );
                }
            }
        }

        protected string CleanPath(string path)
        {
            if (String.IsNullOrEmpty(path))
            {
                return path;
            }

            if (path.StartsWith("\""))
            {
                path = path.TrimStart('"');
                path = path.Substring(0, path.IndexOf("\""));
            }

            int pExe = path.ToLower().IndexOf(".exe");
            int pDll = path.ToLower().IndexOf(".dll");
            if (pExe >= 0 || pDll >= 0)
            {
                path = path.Substring(0, (pExe >= 0 ? pExe : pDll) + 4);
            }

            if (path.IndexOf("\\") < 0)
            {
                // It's probably just a dll name, no direct path.
                path = "";
            }

            return path;
        }

        protected void SearchRegistry(RegistryKey root)
        {
            foreach (string keyName in root.GetSubKeyNames())
            {
                try
                {
                    using (RegistryKey key = root.OpenSubKey(keyName))
                    {
                        if (LookForKeys.Contains(keyName.ToLower()))
                        {
                            // We found the right key, let's read its default value.
                            string defaultValue = CleanPath(key.GetValue("")?.ToString());
                            if (!String.IsNullOrEmpty(defaultValue))
                            {
                                if (!File.Exists(defaultValue))
                                {
                                    Logger.Debug(defaultValue + " does not exist");
                                    Findings.Add(new ACLStruct { regPath = key.Name, result = Result.MISSING_PATH, filePath = defaultValue });
                                }
                                else
                                {
                                    if (Helper.UserACL.HasAccess(new FileInfo(defaultValue), System.Security.AccessControl.FileSystemRights.Modify))
                                    {
                                        Logger.Debug(defaultValue + " can be modified");
                                        Findings.Add(new ACLStruct { regPath = key.Name, result = Result.MODIFY, filePath = defaultValue });
                                    }

                                    if (Helper.UserACL.HasAccess(new FileInfo(defaultValue), System.Security.AccessControl.FileSystemRights.Write))
                                    {
                                        Logger.Debug(defaultValue + " can be written");
                                        Findings.Add(new ACLStruct { regPath = key.Name, result = Result.WRITE, filePath = defaultValue });
                                    }

                                    if (Helper.UserACL.HasAccess(new FileInfo(defaultValue), System.Security.AccessControl.FileSystemRights.Delete))
                                    {
                                        Logger.Debug(defaultValue + " can be deleted");
                                        Findings.Add(new ACLStruct { regPath = key.Name, result = Result.DELETE, filePath = defaultValue });
                                    }

                                    if (Helper.UserACL.HasAccess(new FileInfo(defaultValue), System.Security.AccessControl.FileSystemRights.TakeOwnership))
                                    {
                                        Logger.Debug(defaultValue + " can be owned");
                                        Findings.Add(new ACLStruct { regPath = key.Name, result = Result.OWNERSHIP, filePath = defaultValue });
                                    }
                                }
                            }
                        }

                        SearchRegistry(key);
                    }
                } catch(Exception ex)
                {
                    // Most likely a security exception, let's ignore it just like we ignore all other life problems.
                }
            }
        }
    }
}
