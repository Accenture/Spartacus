using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static Spartacus.Modes.PROXY.PrototypeDatabaseGeneration;

namespace Spartacus.Modes.PROXY.PrototypeParsers
{
    class FunctionProcessor
    {
        public Dictionary<string, List<FunctionPrototype>> ProcessRawFunctions(Dictionary<string, List<RawFunctionPrototype>> headerFiles)
        {
            Dictionary<string, List<FunctionPrototype>> prototypes = new();

            foreach (KeyValuePair<string, List<RawFunctionPrototype>> header in headerFiles.OrderBy(x => x.Key))
            {
                Logger.Verbose("Post-processing " + header.Key);

                if (!prototypes.ContainsKey(header.Key))
                {
                    prototypes[header.Key] = new();
                }

                foreach (RawFunctionPrototype rawFunction in header.Value)
                {
                    FunctionPrototype function = ParseRawPrototype(rawFunction);
                    function.rawFunction = rawFunction;
                    function.returnType = rawFunction.returnType;

                    if (!String.IsNullOrEmpty(function.returnType))
                    {
                        prototypes[header.Key].Add(function);
                    }
                }
            }

            return prototypes;
        }

        protected FunctionPrototype ParseRawPrototype(RawFunctionPrototype rawFunction)
        {
            FunctionPrototype function = new();

            function.name = GetFuctionFromDeclaration(rawFunction.GetPrototypeAsString());
            
            string functionRawArguments = GetArgumentsFromDeclaration(rawFunction.GetPrototypeAsString());
            function.arguments = ParseFunctionArguments(functionRawArguments);

            return function;
        }

        protected string GetFuctionFromDeclaration(string declaration)
        {
            string name = "";
            int p = declaration.IndexOf('(');
            if (p > -1)
            {
                name = declaration.Substring(0, p).Trim();
            }

            return name;
        }

        protected string GetArgumentsFromDeclaration(string declaration)
        {
            string arguments = "";
            int p = declaration.IndexOf('(');
            if (p > -1)
            {
                arguments = declaration.Substring(p).Trim();
            }

            return arguments;
        }

        protected List<FunctionArgument> ParseFunctionArguments(string declaration)
        {
            List<FunctionArgument> arguments = new();

            declaration = declaration
                .Replace(" * ", " *")               // Make sure the pointer sticks to the variable's name.
                .Replace(" ** ", " **")               // Make sure the pointer sticks to the variable's name.
                .Trim(new char[] { '(', ')' })      // Remove parentheses.
                .Trim();

            // Remove any additional annotations like "_In_range_(low, hi)" which may include a comma that will break everything.
            declaration = Regex.Replace(declaration, @"\(.+?\)", "");

            List<string> rawArguments = declaration
                .Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).ToList()    // Split each argument.
                .Select(x => x.Trim()).ToList();                                                // And cleanup.

            foreach (string rawArgument in rawArguments)
            {
                FunctionArgument argument = new();

                string[] items = rawArgument.Split(' ');
                switch (items.Length)
                {
                    case 1:
                        argument.type = items[0].Trim();
                        break;
                    default:
                        argument.name = items.Last().Trim();
                        argument.type = items[items.Length - 2].Trim();
                        break;
                }

                arguments.Add(argument);
            }

            return arguments;
        }
    }
}
