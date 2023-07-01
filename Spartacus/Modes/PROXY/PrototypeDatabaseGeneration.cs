using Spartacus.Modes.PROXY.PrototypeParsers;
using Spartacus.Spartacus.CommandLine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Spartacus.Modes.PROXY
{
    class PrototypeDatabaseGeneration
    {
        public struct RawFunctionPrototype
        {
            public string returnType;

            public List<string> prototype;

            public readonly string GetPrototypeAsString()
            {
                // Replace consecutive spaces with single ones.
                return Regex
                    .Replace(String.Join(" ", prototype).Replace("\t", " "), @"\s{2,}", " ")
                    .Trim(';');
            }
        }

        public struct FunctionArgument
        {
            public string name;

            public string type;

            public readonly string GetDeclaration()
            {
                return type + " " + name;
            }
        }

        public struct FunctionPrototype
        {
            public RawFunctionPrototype rawFunction;

            public string returnType;

            public string name;

            public List<FunctionArgument> arguments;

            public readonly string GetFunctionDeclaration()
            {
                return String.Join(", ", arguments.Select(x => x.GetDeclaration()).ToArray());
            }
        }

        public void Run()
        {
            Logger.Info("Searching for *.h files in " + RuntimeData.Path);
            List<string> headerFiles = Directory.GetFiles(RuntimeData.Path, "*.h", SearchOption.AllDirectories).ToList();
            Logger.Info("Found " + headerFiles.Count + " header files");

            HeaderFileProcessor headerProcessor = new();
            FunctionProcessor functionProcessor = new();

            Dictionary<string, List<RawFunctionPrototype>> rawFunctions = headerProcessor.ProcessHeaderFiles(headerFiles);
            Logger.Info("Loaded " + rawFunctions.Count + " header files");

            Dictionary<string, List<FunctionPrototype>> finalFunctions = functionProcessor.ProcessRawFunctions(rawFunctions);

            do
            {
                try
                {
                    SaveToCSV(finalFunctions);
                    break;
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

        protected void SaveToCSV(Dictionary<string, List<FunctionPrototype>> headerFiles)
        {
            Logger.Info("Saving to CSV at " + RuntimeData.CSVFile);
            using (StreamWriter stream = File.CreateText(RuntimeData.CSVFile))
            {
                stream.WriteLine(string.Format("File,Return Type,Signature"));
                foreach (KeyValuePair<string, List<FunctionPrototype>> item in headerFiles)
                {
                    foreach (FunctionPrototype prototype in item.Value)
                    {
                        stream.WriteLine(
                            String.Format(
                                "\"{0}\",\"{1}\",\"{2}\",\"{3}\"",
                                item.Key,
                                prototype.returnType.Replace("\"", "\"\""),
                                prototype.name.Replace("\"", "\"\""),
                                prototype.GetFunctionDeclaration().Replace("\"", "\"\"")
                            )
                        );
                    }
                }
            }
        }
    }
}
