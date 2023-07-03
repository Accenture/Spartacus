using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static Spartacus.Modes.PROXY.PrototypeDatabaseGeneration;

namespace Spartacus.Modes.PROXY.PrototypeParsers
{
    class HeaderFileProcessor
    {
        private enum PoorMansReplaceMethod
        {
            STARTS_WITH = 1,
            CONTAINS = 2,
            CLOSING_PARENTHESIS = 3
        }

        public Dictionary<string, List<RawFunctionPrototype>> ProcessHeaderFiles(List<string> headerFiles)
        {
            Logger.Info("Processing files...");
            Dictionary<string, List<RawFunctionPrototype>> processedFiles = new();

            foreach (string file in headerFiles)
            {
                Logger.Verbose("Processing " + file);
                List<RawFunctionPrototype> prototypes = ExtractPrototypesFromFile(file);

                if (prototypes.Count > 0)
                {
                    string headerFilename = Path.GetFileName(file);
                    if (!processedFiles.ContainsKey(headerFilename))
                    {
                        processedFiles[headerFilename] = new();
                    }
                    processedFiles[headerFilename].AddRange(prototypes);
                }
            }

            return processedFiles;
        }

        protected List<RawFunctionPrototype> ExtractPrototypesFromFile(string file)
        {
            List<RawFunctionPrototype> prototypes = new();
            string[] fileContents = File.ReadAllLines(file);

            prototypes.AddRange(ExtractPrototypes_STDAPI(fileContents));
            prototypes.AddRange(ExtractPrototypes_WINAPI(fileContents));

            return prototypes;
        }

        protected string RemoveInlineComment(string line)
        {
            int p = line.IndexOf("//");
            if (p > -1)
            {
                line = line.Substring(0, p).Trim();
            }
            return line;
        }

        protected List<RawFunctionPrototype> ExtractPrototypes_STDAPI(string[] fileContents)
        {
            List<RawFunctionPrototype> prototypes = new();

            List<string> prototype = new();
            string returnType = "";

            for (int i = 0; i < fileContents.Length; i++)
            {
                string line = fileContents[i].Trim();

                if (line.StartsWith("#") || line.StartsWith("//"))
                {
                    // Line is a comment or a #define etc expression.
                    continue;
                }

                // Remove any inline-comments.
                line = RemoveInlineComment(line);

                if (line.StartsWith("STDAPI"))
                {
                    if (line == "STDAPI")
                    {
                        /*
                         * Probably looks like this:
                         * 
                            STDAPI
                            RoReportFailedDelegate(
                                _In_ IUnknown* punkDelegate,
                                _In_ IRestrictedErrorInfo* pRestrictedErrorInfo
                                );
                         */

                        returnType = "HRESULT";
                        prototype = new();
                        prototype.AddRange(PopulateRemainingPrototype(fileContents, i + 1));

                        returnType = CleanReturnType(returnType);
                        prototypes.Add(new RawFunctionPrototype { returnType = returnType, prototype = prototype });
                    }
                    else
                    {
                        /* 
                            This can either start with STDAPI_ or with "STDAPI ", depending on the format. 

                            One format:

                            STDAPI_(VOID) AmsiCloseSession(
                                _In_  HAMSICONTEXT amsiContext,
                                _In_  HAMSISESSION amsiSession);

                            Second format:

                            STDAPI AmsiOpenSession(
                                _In_  HAMSICONTEXT amsiContext,
                                _Out_ HAMSISESSION* amsiSession);
                         */
                        Match match = Regex.Match(line, @"STDAPI_\((.*?)\)");
                        prototype = new();
                        if (match.Success)
                        {
                            returnType = match.Groups[1].Value.Trim();
                            // Remove the return type from the string.
                            line = line.Replace(match.Value, "").Trim();
                        }
                        else
                        {
                            returnType = "HRESULT";
                            line = line.Replace("STDAPI", "").Trim();
                        }

                        if (!String.IsNullOrWhiteSpace(line))
                        {
                            prototype.Add(line.Trim());
                        }

                        if (!line.Trim().EndsWith(";"))
                        {
                            prototype.AddRange(PopulateRemainingPrototype(fileContents, i + 1));
                        }

                        returnType = CleanReturnType(returnType);
                        prototypes.Add(new RawFunctionPrototype { returnType = returnType, prototype = prototype });
                    }
                }
            }

            return prototypes;
        }

        protected List<RawFunctionPrototype> ExtractPrototypes_WINAPI(string[] fileContents)
        {
            List<RawFunctionPrototype> prototypes = new();

            List<string> prototype = new();
            string returnType = "";

            for (int i = 0; i < fileContents.Length; i++)
            {
                string line = fileContents[i].Trim();

                if (line.StartsWith("#") || line.StartsWith("//"))
                {
                    // Line is a comment or a #define etc expression.
                    continue;
                }
                else if (line.Contains("WINAPI_") || line.Contains("(WINAPI") || line.Contains("typedef") || line.Contains("static "))
                {
                    continue;
                }

                // Remove any inline-comments.
                line = RemoveInlineComment(line);

                if (line.Contains("WINAPI"))
                {
                    if (line == "WINAPI")
                    {
                        /*
                         * If the line only has WINAPI then the return type is the line BEFORE that one.
                         * This is how it looks:
                         * 
                            UINT
                            WINAPI
                            GetTimeZoneList (
                                __out_opt TIME_ZONE_INFORMATION_WITH_ID *rgTimeZoneList,
                                __in const UINT cTimeZoneList
                            );
                         */
                        returnType = fileContents[i - 1].Trim();
                        if (returnType.StartsWith("#") || returnType.StartsWith("_") || returnType.StartsWith("const"))
                        {
                            returnType = "";
                        }
                        else
                        {
                            prototype = new();
                            prototype.AddRange(PopulateRemainingPrototype(fileContents, i + 1));

                            returnType = CleanReturnType(returnType);
                            prototypes.Add(new RawFunctionPrototype { returnType = returnType, prototype = prototype });
                        }
                    }
                    else
                    {
                        prototype = new();
                        /*
                         * If the line isn't just "WINAPI", the return type is the word before it, like:
                         * 
                            NTSTATUS WINAPI
                            BCryptResolveProviders(
                                _In_opt_ LPCWSTR pszContext,
                                _In_opt_ ULONG dwInterface,
                                _In_opt_ LPCWSTR pszFunction,
                                _In_opt_ LPCWSTR pszProvider,
                                _In_ ULONG dwMode,
                                _In_ ULONG dwFlags,
                                _Inout_ ULONG* pcbBuffer,
                                _Inout_
                                    _When_(_Old_(*ppBuffer) != NULL, _At_(*ppBuffer, _Out_writes_bytes_to_(*pcbBuffer, *pcbBuffer)))
                                    _When_(_Old_(*ppBuffer) == NULL, _Outptr_result_bytebuffer_all_(*pcbBuffer))
                                PCRYPT_PROVIDER_REFS *ppBuffer);

                            or like:

                            HRESULT WINAPI RunSetupCommandA(HWND hWnd, LPCSTR szCmdName, LPCSTR szInfSection, LPCSTR szDir, LPCSTR lpszTitle, HANDLE* phEXE, DWORD dwFlags, LPVOID pvReserved);

                            or like:

                            WINCOMMCTRLAPI BOOL   WINAPI DSA_DeleteAllItems(_Inout_ HDSA hdsa);

                            or like:

                            BOOL
                            WINAPI FormatVerisignExtension(
                                DWORD,
                                DWORD,
                                DWORD,
                                void *,
                                LPCSTR,
                                const BYTE *,
                                DWORD,
                                void * pbFormat,
                                DWORD * pcbFormat);
                         */

                        string[] parts;
                        if (line.StartsWith("WINAPI"))
                        {
                            // The return type is one line above.
                            returnType = fileContents[i - 1].Trim();
                            parts = line.Split(new string[] { "WINAPI" }, StringSplitOptions.RemoveEmptyEntries);
                            prototype.Add(parts[0].Trim());

                            if (!parts[0].Trim().EndsWith(";"))
                            {
                                prototype.AddRange(PopulateRemainingPrototype(fileContents, i + 1));
                            }
                        }
                        else
                        {
                            // Split by "WINAPI" to see if we are left with a 2 or 3 sized array.
                            parts = line.Split(new string[] { "WINAPI" }, StringSplitOptions.RemoveEmptyEntries);

                            /*
                             * If size is 1, we only have the return type in this statement.
                             * If size is 2, we have part of or a full declaration statement as well.
                             */
                            returnType = parts[0].Trim();
                            if (parts.Length == 2)
                            {
                                prototype.Add(parts[1].Trim());
                                if (!parts[1].Trim().EndsWith(";"))
                                {
                                    prototype.AddRange(PopulateRemainingPrototype(fileContents, i + 1));
                                }
                            }
                            else
                            {
                                prototype.AddRange(PopulateRemainingPrototype(fileContents, i + 1));
                            }
                        }

                        returnType = CleanReturnType(returnType);
                        prototypes.Add(new RawFunctionPrototype { returnType = returnType, prototype = prototype });
                    }


                }
            }

            return prototypes;
        }

        protected string CleanReturnType(string returnType)
        {
            // If it's a single word, return it immediately.
            if (!returnType.Contains(" "))
            {
                returnType = returnType.TrimStart(new char[] { '_', '*', '/' }).Trim();
                return returnType;
            }
            else if (returnType.StartsWith("friend"))
            {
                // This one is too complicated to parse at this point, am considering it an edge case.
                return "";
            }

            List<string> replaceClosingParenthesis = new()
            {
                "_Success_(",
                "__declspec(",
            };
            foreach (string value in replaceClosingParenthesis)
            {
                returnType = PoorMansReplaceFunction(returnType, value, "", PoorMansReplaceMethod.CLOSING_PARENTHESIS);
                if (!returnType.Contains(" "))
                {
                    break;
                }
            }

            List<string> replaceContains = new()
            {
                "extern ",
                "_Check_return_ ",
                "EXTERN_C "
            };
            foreach (string value in replaceContains)
            {
                returnType = PoorMansReplaceFunction(returnType, value, "", PoorMansReplaceMethod.CONTAINS);
                if (!returnType.Contains(" "))
                {
                    break;
                }
            }

            List<string> replaceStartsWith = new()
            {
                "\"C\"",
                "const ",
                "struct ",
                "CLFSUSER_API",
                "WINCOMMCTRLAPI",
                "WINADVAPI",
                "WINGDIAPI",
                "WINHTTPAPI",
                "WINMMAPI",
                "WINSCARDAPI",
                "WINSHELLAPI",
                "WINUSERAPI",
                "INTSHCUTAPI",
                "WINCOMMCTRLAPI",
                "WINBASEAPI",
                "AMOVIEAPI",
            };
            foreach (string value in replaceStartsWith)
            {
                returnType = PoorMansReplaceFunction(returnType, value, "", PoorMansReplaceMethod.STARTS_WITH);
                if (!returnType.Contains(" "))
                {
                    break;
                }
            }

            if (returnType.EndsWith("*"))
            {
                int numberOfSpaces = returnType.Count(s => s == ' ');
                if (numberOfSpaces == 1)
                {
                    return returnType;
                }
            }

            return returnType;
        }

        // Don't ask.
        private string PoorMansReplaceFunction(string inputString, string searchFor, string replaceWith, PoorMansReplaceMethod method)
        {
            int p, p2;
            switch (method)
            {
                case PoorMansReplaceMethod.CONTAINS:
                    p = inputString.IndexOf(searchFor, StringComparison.InvariantCultureIgnoreCase);
                    if (p != -1)
                    {
                        inputString = inputString.Replace(inputString.Substring(p, searchFor.Length), replaceWith).Trim();
                    }
                    break;
                case PoorMansReplaceMethod.STARTS_WITH:
                    if (inputString.StartsWith(searchFor, StringComparison.InvariantCultureIgnoreCase))
                    {
                        inputString = inputString.Substring(searchFor.Length).Trim();
                    }
                    break;
                case PoorMansReplaceMethod.CLOSING_PARENTHESIS:
                    p = inputString.IndexOf(searchFor, StringComparison.InvariantCultureIgnoreCase);
                    if (p != -1)
                    {
                        p2 = inputString.IndexOf(")", p);
                        if (p2 != -1)
                        {
                            string foundString = inputString.Substring(p, p2 - p + 1).Trim();
                            inputString = inputString.Replace(foundString, replaceWith).Trim();
                        }
                    }
                    break;
            }

            return inputString.Trim();
        }

        protected List<string> PopulateRemainingPrototype(string[] fileContents, int startFrom)
        {
            List<string> prototype = new();

            for (int k = startFrom; k < fileContents.Length; k++)
            {
                string line = fileContents[k].Trim();

                // Remove any inline-comments.
                line = RemoveInlineComment(line);

                prototype.Add(line);
                if (line.EndsWith(";"))
                {
                    break;
                }
            }

            return prototype;
        }
    }
}
