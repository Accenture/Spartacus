using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spartacus.Spartacus.Models
{
    class FunctionSignature
    {
        public string Name { get; set; }

        public string Return { get; set; }

        public string Signature { get; set; }

        public FunctionSignature(string Name)
        {
            this.Name = Name;
        }

        public struct Parameter
        {
            public string Name { get; set; }
            public string Type { get; set; }

            public int Ordinal { get; set; }
        }

        public List<Parameter> Parameters = new List<Parameter>();

        public void CreateParameter(int Ordinal, string Name, string Type)
        {
            Parameters.Add(
                new Parameter
                {
                    Ordinal = Ordinal,
                    Name = Name,
                    Type = Type
                }
            );
        }

        public void AddParameter(Parameter parameter)
        {
            Parameters.Add(parameter);
        }

        public string GetProxyName()
        {
            return Name + "_Proxy";
        }

        public string GetArgumentDeclaration()
        {
            List<string> arguments = new List<string>();
            foreach (Parameter parameter in Parameters)
            {
                arguments.Add(MapType(parameter.Type) + " " + parameter.Name);
            }
            return String.Join(", ", arguments.ToArray());
        }

        protected string MapType(string value)
        {
            switch (value.ToLower())
            {
                case "uint":
                    value = "unsigned int";
                    break;
                case "uint *":
                    value = "unsigned int *";
                    break;
                case "ushort":
                    value = "unsigned short";
                    break;
                case "ushort *":
                    value = "unsigned short *";
                    break;
                case "longlong":
                case "ulonglong":
                case "longlong *":
                case "ulonglong *":
                case "longlong * *":
                case "ulonglong * *":
                    value = value.ToUpper();
                    break;
            }
            return value;
        }

        public string GetArgumentCall()
        {
            List<string> arguments = new List<string>();
            foreach (Parameter parameter in Parameters)
            {
                arguments.Add(parameter.Name);
            }
            return String.Join(", ", arguments.ToArray());
        }

        public string GetTypedefDeclaration()
        {
            return $"typedef {Return}(*{Name}_Type)(" + GetArgumentDeclaration() + ")";
        }

        public string GetProxyFunctionCode(bool addDebugLogStatement)
        {
            List<string> code = new List<string>();

            code.Add(Return + " " + GetProxyName() + "(" + GetArgumentDeclaration() + ")");
            code.Add("{");
            if (addDebugLogStatement)
            {
                code.Add("\t" + $"DebugToFile(\"{Name}\");");
            }
            code.Add("\t" + $"{Name}_Type original = ({Name}_Type)GetProcAddress(hModule, \"{Name}\");");
            code.Add("\t" + $"return original({GetArgumentCall()});");
            code.Add("}");

            return String.Join("\r\n", code.ToArray());
        }

        public string GetProxyFunctionCode()
        {
            return GetProxyFunctionCode(false);
        }
    }
}
