﻿using System;
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

                    if (!String.IsNullOrEmpty(function.returnType))
                    {
                        // Check if this function has already been parsed. Sometimes there will be multiple
                        // definitions of a specific function based on OS. We don't really care about that
                        // because this feature is mostly to help out, rather than be 100% reliable.

                        int findFunction = prototypes[header.Key].Count(s => s.name == function.name);
                        if (findFunction == 0)
                        {
                            prototypes[header.Key].Add(function);
                        }
                    }
                }
            }

            return prototypes;
        }

        protected FunctionPrototype ParseRawPrototype(RawFunctionPrototype rawFunction)
        {
            FunctionPrototype function = new();

            function.rawFunction = rawFunction;
            function.returnType = rawFunction.returnType;
            function.name = GetFuctionFromDeclaration(rawFunction.GetPrototypeAsString());
            
            string functionRawArguments = GetArgumentsFromDeclaration(rawFunction.GetPrototypeAsString());
            function.arguments = ParseFunctionArguments(functionRawArguments);

            return OneLastCheck(function);
        }

        protected FunctionPrototype OneLastCheck(FunctionPrototype function)
        {
            // Although we've tried to process all header files to the best of our abilities
            // there may still be some declarations that were too complex to parse. For example
            // The ones that use annotations especially like "_When"_ that adds conditional
            // statements to such declarations. This function will check if any special characters
            // are still present in the definition and "invalidate" the function (empty the returnType).
            // These characters include =, +, -, etc.

            List<string> invalidCharacters = new() { "=", "-", "+", "(", ")", "\"", "'", "#", "/", "\\", "<", ">", "|", "!" };

            foreach (string character in invalidCharacters)
            {
                if (function.GetFunctionDeclaration().Contains(character) || function.name.Contains(character) || function.returnType.Contains(character))
                {
                    function.returnType = "";
                    break;
                }
            }

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
                .Replace(" * ", "* ")               // Make sure the pointer sticks to the type's name.
                .Replace(" ** ", "** ")             // Make sure the pointer sticks to the type's name.
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

                string[] items = rawArgument
                    .Replace(" OPTIONAL", " ")
                    .Trim()
                    .Split(' ');
                switch (items.Length)
                {
                    case 1:
                        argument.name = "";
                        argument.type = items[0].Trim();
                        break;
                    default:
                        argument.name = items.Last().Trim();
                        argument.type = items[items.Length - 2].Trim();

                        // If a pointer exists, move it to the type.
                        if (argument.name.StartsWith("*"))
                        {
                            argument.name = argument.name.Substring(1);
                            argument.type += "*";

                            // Maybe it's a double pointer, sigh.
                            if (argument.name.StartsWith("*"))
                            {
                                argument.name = argument.name.Substring(1);
                                argument.type += "*";
                            }
                        }
                        break;
                }

                arguments.Add(argument);
            }

            return arguments;
        }
    }
}
