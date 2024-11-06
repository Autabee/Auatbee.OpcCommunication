using Opc.Ua;
using System;
using System.Linq;
using System.Xml;

namespace Autabee.OpcToClass
{
    public class OpcStructTemplate : OpcTemplate
    {
        public XmlNode[] nodes = null;

        public string TypeName = string.Empty;
        public string NamespaceURI = string.Empty;
        public ExpandedNodeId TypeId = null;
        public ExpandedNodeId BinaryEncoding = null;
        public ExpandedNodeId XmlEncoding = null;

        public OpcStructTemplate BaseType = null;

        public Field[] Fields = new Field[0];
        public FunctionDefinitions[] Functions = new FunctionDefinitions[0];

        override public string[] GetScriptNameSpaces()
            => namespaces;

        static public string[] namespaces = new string[] { "Opc.Ua" };

        override public string GetScript(GeneratorDataSet settings)
            => this.GenerateClassScript(settings);

        override public string GetScriptAsFile(GeneratorDataSet settings)
            => "using Opc.Ua;\n\n" + this.GenerateClassScript(settings);
    }
}
