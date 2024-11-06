using System;
using System.Linq;
using System.Xml;

namespace Autabee.OpcToClass
{
    public class OpcEnumTemplate : OpcTemplate, IOpcSharperTemplate
    {
        public XmlNode[] nodes = null;

        public string TypeName = string.Empty;
        public string NamespaceURI = string.Empty;
        //public bool BinaryEncoding = false;
        //public bool XmlEncoding = false;

        //public OpcStructTemplate BaseType = null;

        public Field[] Fields = new Field[0];
        //public FunctionDefinitions[] Functions = new FunctionDefinitions[0];

        public static string GenerateEnumFileContent(OpcEnumTemplate template, GeneratorDataSet dataset)
            => "using Opc.Ua;\n\n" + GenerateEnumScript(template, dataset);


        public static string GenerateEnumScript(OpcEnumTemplate template, GeneratorDataSet dataset)
        {
            string ns = string.IsNullOrWhiteSpace(template.NameSpace)
                ? dataset.baseNamespace
                : $"{dataset.baseNamespace}.{template.NameSpace}";


            string classData = $"\nnamespace {ns}\n{{";
            classData += $"\n\tpublic enum {template.ClassName}\n\t{{";
            foreach (var field in template.Fields)
            {
                classData += $"\n\t\t{field.Name} = {field.Value},";
            }
            classData = classData.Remove(classData.Length - 1);
            classData += "\n\t}";
            classData += "\n}";


            return classData;
        }

        override public string[] GetScriptNameSpaces()
            => namespaces;

        static public string[] namespaces = new string[] { "Opc.Ua" };

        override public string GetScript(GeneratorDataSet settings)
            => GenerateEnumScript(this, settings);

        override public string GetScriptAsFile(GeneratorDataSet settings)
            => GenerateEnumFileContent(this, settings);
    }
}
