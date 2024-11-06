using Autabee.Communication.ManagedOpcClient;
using Autabee.Communication.ManagedOpcClient.ManagedNode;
using Newtonsoft.Json.Linq;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Schema.Binary;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace Autabee.OpcToClass
{
    public static class OpcToCSharpGenerator
    {
        public static string GetDecoder(XmlNode node, string nsPrefix, string[] KnownEnums, string decoderName = "decoder")
        {
            var name = GetCorrectedName(node);
            var type = (string)node.Attributes["TypeName"].Value;
            var typename = GetCorrectedTypeName(node, nsPrefix);
            return GetDecoder(KnownEnums, decoderName, type, name, typename);
        }

        public static string GetDecoder(Field node, string[] KnownEnums, string decoderName = "decoder")
        {
            return GetDecoder(KnownEnums, decoderName, node.TypeName, node.Name, node.Type);
        }



        private static string GetDecoder(string[] KnownEnums, string decoderName, string type, string name, string typename)
        {
            if (type.Contains("tns:"))
            {
                if (KnownEnums.Contains(typename))
                {
                    return $"({typename}){decoderName}.ReadEnumerated(\"{name}\" ,typeof({typename}))";
                }
                return $"{decoderName}.ReadEncodeable(\"{name}\", typeof({typename})) as {typename}";
            }
            else if (type.Contains("opc:"))
            {
                return $"{decoderName}.Read{type.Split(':')[1]}(\"{name}\")";
            }
            return $"{decoderName}.Unknown({type})";
        }

        public static string GetEncoder(XmlNode node, string nsPrefix, string[] KnownEnums, string encoderName = "encoder")
        {
            var type = (string)node.Attributes["TypeName"].Value;
            var name = GetCorrectedName(node);
            var typename = GetCorrectedTypeName(node, nsPrefix);
            return GetEncoder(KnownEnums, encoderName, type, name, typename);
        }

        private static string GetEncoder(string[] KnownEnums, string encoderName, string type, string name, string typename)
        {
            if (type.Contains("tns:"))
            {
                if (KnownEnums.Contains(typename))
                {
                    return $"{encoderName}.WriteEnumerated(\"{typename}\", this.{name})";
                }
                return $"{encoderName}.WriteEncodeable(\"{name}\", this.{name}, typeof({typename}))";
            }
            else if (type.Contains("opc:"))
            {
                return $"{encoderName}.Write{type.Split(':')[1]}(\"{name}\", this.{name})";
            }
            return $"{encoderName}.Unknown({type})";
        }

        public static string GetEncoder(Field node, string[] KnownEnums, string encoderName = "encoder")
        {
            return GetEncoder(KnownEnums, encoderName, node.TypeName, node.Name, node.Type);
        }


        public static string GetCorrectedName(XmlNode value)
        {
            if (value is XmlAttribute attribute)
            {
                return attribute.Value
                    .Replace("&quot;", "")
                    .Replace("\"", "");
            }
            return ((string)value.Attributes["Name"].Value)
                    .Replace("&quot;", "")
                    .Replace("\"", "");
        }

        public static string GetCorrectedTypeName(XmlNode value, string nsPrefix)
        {
            return GetCorrectedTypeName((string)value.Attributes["TypeName"].Value, nsPrefix);
        }
        public static string GetCorrectedTypeName(object value, string nsPrefix)
        {
            switch (value)
            {
                case uint numeric:
                    return GetCorrectedTypeName(numeric, nsPrefix);
                case string str:
                    return GetCorrectedTypeName(str, nsPrefix);
                default:
                    return $"Unknown<{value}>";
            }
        }

        public static string GetCorrectedTypeName(uint value, string nsPrefix)
        {

            return value switch
            {
                DataTypes.Boolean => "bool",
                DataTypes.Byte => "byte",
                DataTypes.SByte => "sbyte",
                DataTypes.Int32 => "int",
                DataTypes.UInt32 => "uint",
                DataTypes.Float => "float",
                DataTypes.Double => "double",
                DataTypes.String => "string",
                DataTypes.Int16 => "short",
                DataTypes.UInt16 => "ushort",
                DataTypes.Int64 => "long",
                DataTypes.UInt64 => "ulong",
                DataTypes.ByteString => "byte[]",
                DataTypes.DateTime => "DateTime",
                DataTypes.Guid => "Guid",
                DataTypes.LocalizedText => "LocalizedText",
                DataTypes.QualifiedName => "QualifiedName",
                DataTypes.NodeId => "NodeId",
                DataTypes.ExpandedNodeId => "ExpandedNodeId",
                DataTypes.StatusCode => "StatusCode",
                DataTypes.XmlElement => "XmlElement",
                DataTypes.DataValue => "DataValue",
                _ => GetCorrectedTypeNameFromDataTypes(value)
            };
        }

        public static string GetCorrectedTypeNameFromDataTypes(uint value)
        {
            var result = typeof(DataTypes).GetFields().Where(f => f.FieldType == typeof(uint)).ToList().Where(f => ((uint)f.GetValue(null) == value));

            if (result.Count() == 1)
                return "Opc.Ua." + result.First().Name;


            return $"Unknown<{value}>";

        }

        public static string GetCorrectedTypeName(string value, string nsPrefix)
        {
            if (value.Contains("opc:"))
            {
                switch (value)
                {
                    case "opc:Boolean": return "bool";
                    case "opc:Byte": return "byte";
                    case "opc:Int32": return "int";
                    case "opc:UInt32": return "uint";
                    case "opc:Float": return "float";
                    case "opc:Double": return "double";
                    case "opc:String": return "string";
                    case "opc:Int16": return "short";
                    case "opc:UInt16": return "ushort";
                    case "opc:Int64": return "long";
                    case "opc:UInt64": return "ulong";
                    case "opc:ByteString": return "byte[]";
                    case "opc:DateTime": return "DateTime";
                    case "opc:Guid": return "Guid";

                    case "opc:LocalizedText": return "LocalizedText";
                    case "opc:QualifiedName": return "QualifiedName";
                    case "opc:NodeId": return "NodeId";
                    case "opc:ExpandedNodeId": return "ExpandedNodeId";
                    case "opc:StatusCode": return "StatusCode";
                    case "opc:XmlElement": return "XmlElement";
                    case "opc:ExtensionObject": return "ExtensionObject";
                    case "opc:DataValue": return "DataValue";
                }
            }
            else
            {
                var temp = value
                    .Replace("&quot;", "")
                    .Replace("\"", "")
                    .Split(':').Last().Split('.');
                if (temp.Length > 1)
                {
                    value = nsPrefix + string.Join($".{nsPrefix}", temp.Take(temp.Length - 1));
                    value += "." + temp.Last();
                }
                else
                {
                    value = temp.Last();
                }
                return value;

            }
            Console.WriteLine("Did not find typename: " + value);

            return $"Unknown<{value}>";
        }


        public static Type GetCSharpTypeOfOpc(string opcTypeString)
        {
            switch (opcTypeString)
            {
                case "opc:Boolean": return typeof(bool);
                case "opc:Byte": return typeof(byte);
                case "opc:Int32": return typeof(int);
                case "opc:UInt32": return typeof(uint);
                case "opc:Float": return typeof(float);
                case "opc:Double": return typeof(double);
                case "opc:String": return typeof(string);
                case "opc:Int16": return typeof(short);
                case "opc:UInt16": return typeof(ushort);
                case "opc:Int64": return typeof(long);
                case "opc:UInt64": return typeof(ulong);
                case "opc:ByteString": return typeof(byte[]);
                case "opc:DateTime": return typeof(DateTime);
                case "opc:Guid": return typeof(Guid);
                case "opc:LocalizedText": return typeof(LocalizedText);
                case "opc:QualifiedName": return typeof(QualifiedName);
                case "opc:NodeId": return typeof(NodeId);
                case "opc:ExpandedNodeId": return typeof(ExpandedNodeId);
                case "opc:StatusCode": return typeof(StatusCode);
                case "opc:XmlElement": return typeof(XmlElement);
                case "opc:ExtensionObject": return typeof(ExtensionObject);
                case "opc:DataValue": return typeof(DataValue);
            }
            return typeof(object);
        }

        static public void GenerateCsharpProject(GeneratorSettings settings)
        {


            Directory.CreateDirectory(settings.baseLocation);

            // generate cs libary project file in generate folder with the basetype name
            var fileContent =
      @"<Project Sdk=""Microsoft.NET.Sdk"">

	<PropertyGroup>
		<TargetFrameworks>netstandard2.0;net48;net6.0</TargetFrameworks>
	</PropertyGroup>

	<ItemGroup>
	  <PackageReference Include=""Autabee.Communication.ManagedOpcClient"" Version=""0.*"" />
	</ItemGroup>

</Project>";

            CreateFile(Path.Combine(settings.baseLocation, settings.baseNamespace + ".csproj"), fileContent);
        }

        public static void GenerateTypeFiles(XmlDocument xmlDocument, GeneratorSettings settings)
        {
            Directory.CreateDirectory(settings.baseLocation);
            foreach (var item in GenerateClassFileConents(xmlDocument, settings))
            {
                var (name, scriptContent) = (item.Key, item.Value);
                var split = name.Split('.');

                var namespaceName = string.Join('.' + settings.nameSpacePrefix, split.Take(split.Length - 1));
                var className = split.Last();
                var fileName = className + ".cs";

                string filePath;
                if (string.IsNullOrWhiteSpace(namespaceName))
                {
                    filePath = Path.Combine(settings.baseLocation, fileName);
                }
                else
                {
                    Directory.CreateDirectory(Path.Combine(settings.baseLocation, settings.nameSpacePrefix + string.Join($"\\{settings.nameSpacePrefix}", split.Take(split.Length - 1))));
                    filePath = Path.Combine(settings.baseLocation, settings.nameSpacePrefix + string.Join($"\\{settings.nameSpacePrefix}", split.Take(split.Length - 1)), fileName);
                }

                CreateFile(filePath, scriptContent);
            }
        }


        private static Dictionary<string, string> GenerateClassFileConents(XmlDocument xmlDocument, GeneratorSettings settings)
        {
            Dictionary<string, string> scripts = new Dictionary<string, string>();
            var enumTypes = xmlDocument.GetElementsByTagName("opc:EnumeratedType");
            string[] enums = new string[enumTypes.Count];
            for (int i = 0; i < enumTypes.Count; i++)
            {
                string name, scriptContent;
                GetEnumScript(settings, enumTypes, i, out name, out scriptContent);

                scripts[name] = scriptContent;
                enums[i] = name;
            }

            foreach (XmlNode typedict in xmlDocument.GetElementsByTagName("opc:TypeDictionary"))
            {
                string targetNamespace = typedict.Attributes["TargetNamespace"].Value;
                foreach (XmlNode value in typedict.ChildNodes)
                {
                    if (value.Name == "opc:StructuredType")
                    {
                        string name, scriptContent;
                        GetStructScript(settings, enums, targetNamespace, value, out name, out scriptContent);

                        scripts[name] = scriptContent;
                    }
                }
            }

            return scripts;
        }

        private static void GetStructScript(GeneratorSettings settings, string[] enums, string targetNamespace, XmlNode value, out string name, out string scriptContent)
        {
            name = GetCorrectedName(value);
            var split = name.Split('.');
            var namespaceName = string.Join('.' + settings.nameSpacePrefix, split.Take(split.Length - 1));
            namespaceName = string.IsNullOrWhiteSpace(namespaceName)
              ? settings.baseNamespace
              : settings.baseNamespace + '.' + settings.nameSpacePrefix + namespaceName;

            scriptContent = "using Opc.Ua;\n\n"
                + "namespace " + namespaceName + "\n{";
            scriptContent += GenerateClass(settings.nameSpacePrefix, value, split.Last(), targetNamespace, enums).Replace("\n", "\n\t");
            scriptContent += "\n}";
        }

        private static void GetEnumScript(GeneratorSettings settings, XmlNodeList enumTypes, int i, out string name, out string scriptContent)
        {
            XmlNode value = enumTypes[i];
            name = GetCorrectedName(value);
            var split = name.Split('.');
            var namespaceName = string.Join('.' + settings.nameSpacePrefix, split.Take(split.Length - 1));
            namespaceName = string.IsNullOrWhiteSpace(namespaceName)
                ? settings.baseNamespace
                : settings.baseNamespace + '.' + settings.nameSpacePrefix + namespaceName;

            scriptContent = "using Opc.Ua;\n\n"
                + "namespace " + namespaceName + "\n{";
            scriptContent += GenerateEnum(settings.nameSpacePrefix, value, split.Last()).Replace("\n", "\n\t");
            scriptContent += "\n}";
        }

        //private static Dictionary<string, string> GenerateClassScripts(XmlDocument xmlDocument, GeneratorSettings settings)
        //{
        //  List<SyntaxTree> syntaxFactories = new List<SyntaxTree>();
        //  List<Assembly> references = new List<Assembly>() {
        //    typeof(object).GetTypeInfo().Assembly,
        //            typeof(Opc.Ua.AccessLevels).GetTypeInfo().Assembly };
        //  var interactiveLoader = new InteractiveAssemblyLoader();
        //  foreach (var reference in references)
        //  {
        //    interactiveLoader.RegisterDependency(reference);
        //  }
        //  var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
        //    optimizationLevel: OptimizationLevel.Release)
        //    .AddReferences(references)
        //    .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp7_1)


        //  foreach (XmlNode value in xmlDocument.GetElementsByTagName("opc:StructuredType"))
        //  {
        //    var name = GetCorrectedName(value);
        //    var split = name.Split('.');
        //    var namespaceName = string.Join('.' + "ns_", split.Take(split.Length - 1));
        //    namespaceName = string.IsNullOrWhiteSpace(namespaceName)
        //      ? settings.baseNamespace
        //      : settings.baseNamespace + '.' + settings.nsPrefix + namespaceName;


        //    var scriptContent = "namespace " + namespaceName + "\n{";
        //    scriptContent += GenerateClass("Ns", value, split.Last()).Replace("\n", "\n\t");
        //    scriptContent += "\n}";

        //    SyntaxTree tree = SyntaxFactory.ParseSyntaxTree(scriptContent);
        //  }
        //  var builder = CSharpCompilation.Create(settings.baseNamespace);
        //  string path = Path.Combine(Directory.GetCurrentDirectory() + "/Generated/" + settings.baseLocation, settings.baseLocation + ".dll");
        //  var result = builder.Emit(path);
        //  return scripts;
        //}

        private static string GenerateEnum(string nsPrefix, XmlNode value, string className)
        {
            string classData = "\npublic enum " + className
                        + "\n{";
            foreach (XmlNode field in value.ChildNodes)
            {
                if (field.Name == "opc:EnumeratedValue")
                {
                    var fieldName = GetCorrectedName(field);

                    classData += $"\n\t{fieldName} = {field.Attributes["Value"].Value},";
                }
            }
            classData = classData.Remove(classData.Length - 1);
            classData += "\n}";
            return classData;

        }

        private static string GenerateClass(string nsPrefix, XmlNode value, string className, string targetNamespace, string[] KnownEnums = null)
        {
            if (KnownEnums == null) KnownEnums = new string[0];
            string classData = "\npublic class " + className + " : EncodeableObject"
                        + "\n{";
            foreach (XmlNode field in value.ChildNodes)
            {
                if (field.Name == "opc:Field")
                {
                    var fieldName = GetCorrectedName(field);
                    var fieldType = GetCorrectedTypeName(field, nsPrefix);
                    classData += "\n\tpublic " + fieldType + " " + fieldName + " { get; set; }";
                }
            }

            // To Do This does not work always as systems somtimes use struct name with or without qoutations.
            // example: `"value"` vs `value`.
            // might need to read the node itself to get the correct name.

            classData += $"\n\tpublic override ExpandedNodeId TypeId => new ExpandedNodeId(\"TE_{value.Attributes["Name"].Value.Replace("\"", "\\\"")}\", \"{targetNamespace}\");";

            if (value.NamespaceURI == "http://opcfoundation.org/BinarySchema/")
            {
                classData += $"\n\tpublic override ExpandedNodeId BinaryEncodingId => ExpandedNodeId.Parse(\"s=TD_{value.Attributes["Name"].Value.Replace("\"", "\\\"")}\");";
            }
            else
            {
                classData += $"\n\tpublic override ExpandedNodeId BinaryEncodingId => NodeId.Null;";
            }

            if (value.NamespaceURI == "http://opcfoundation.org/XmlSchema/")
            {
                classData += $"\n\tpublic override ExpandedNodeId  XmlEncodingId => ExpandedNodeId.Parse(\"s=TD_{value.Attributes["Name"]}\");";
            }
            else
            {
                classData += $"\n\tpublic override ExpandedNodeId XmlEncodingId => NodeId.Null;";
            }

            //fileContents += $"\n\tExpandedNodeId override TypeId => ExpandedNodeId.Parse(@\"{xmlDocument}\");";

            //Encoder function
            classData += "\n\n\tpublic override void Encode(IEncoder encoder)\n\t{";
            foreach (XmlNode field in value.ChildNodes)
            {
                if (field.Name == "opc:Field")
                {
                    classData += "\n\t\t"
                        + GetEncoder(field, nsPrefix, KnownEnums) + ";";
                }
            }
            classData += "\n\t}";


            //decoder function
            classData += "\n\n\tpublic override void Decode(IDecoder decoder)\n\t{";
            foreach (XmlNode field in value.ChildNodes)
            {
                if (field.Name == "opc:Field")
                {
                    classData += $"\n\t\tthis.{GetCorrectedName(field)} = "
                        + GetDecoder(field, nsPrefix, KnownEnums) + ";";
                }
            }
            classData += "\n\t}";

            classData += "\n}";
            return classData;
        }

        public static void CreateFile(string filePath, string fileContents)
        {
            using (FileStream stream = File.Create(filePath))
            {
                var array = Encoding.UTF8.GetBytes(fileContents.Replace("\n", Environment.NewLine));
                stream.Write(array, 0, array.Length);
                stream.Close();
            }
        }

        static public void GenerateAddressSpace(ReferenceDescriptionCollection referenceNodes, GeneratorSettings settings)
        {
            //FileStream stream;
            var fileContents = "using Opc.Ua;\n\n"
                + "namespace " + settings.baseNamespace + "\n{"
                + "\n\tpublic static class AddressSpace"
                + "\n\t{";

            var filteredList = referenceNodes.ToList().GroupBy(x => x.NodeId.ToString())
            .Select(y => y.First())
            .ToList();



            foreach (var referenceNode in filteredList)
            {
                if (referenceNode.NodeId.IdType != IdType.String) continue;
                string funcName = GetFunctionName(referenceNode.NodeId.Identifier.ToString(), true);
                fileContents += $"\n\t\tpublic static readonly NodeId {funcName} = NodeId.Parse(\"{referenceNode.NodeId.ToString().Replace("\"", "\\\"")}\");";
            }

            fileContents += "\n\t}";
            fileContents += "\n}";

            CreateFile(Path.Combine(settings.baseLocation, "AddressSpace.cs"), fileContents);
        }

        static public void GenerateNodeEntryAddressSpace(AutabeeManagedOpcClient client, ReferenceDescriptionCollection referenceNodes, NodeCollection nodes, XmlDocument[] xmls, GeneratorSettings settings)
        {
            var fileContents = "using Opc.Ua;\n"
                + "using Autabee.Communication.ManagedOpcClient.ManagedNode;\n\n"
                + "namespace " + settings.baseNamespace + "\n{"
                + "\n\tpublic static class NodeEntryAddressSpace"
                + "\n\t{";
            Dictionary<string, FunctionDefinitions> functionDefinitions = new Dictionary<string, FunctionDefinitions>();
            for (int i = 0; i < referenceNodes.Count; i++)
            {
                var referenceNode = referenceNodes[i];
                var nodeData = nodes[i];
                if (referenceNode.NodeId.IdType != IdType.String) continue;
                if (referenceNode.NodeClass != NodeClass.Variable)
                    continue;

                string identifier = referenceNode.TypeDefinition.Identifier.ToString();
                //var node = referenceNode.NodeId.Identifier.ToString()
                //    .Replace("\"", "")
                //    .Replace(".", "_")
                //    .Replace("[", "")
                //    .Replace("]", "");

                string nodetype = String.Empty;

                if (referenceNode.TypeDefinition.IdType == IdType.Numeric && nodeData is VariableNode varNode)
                {
                    nodetype = GetCorrectedTypeName(varNode.DataType.Identifier, settings.nameSpacePrefix);

                    if (nodetype.Contains("Unknown"))
                    {
                        var value = client.ReadValue(nodeData);
                        if (typeof(NodeTypeData).IsInstanceOfType(value) || typeof(EncodeableObject).IsInstanceOfType(value))
                        {
                            if (settings.typeOverrides.TryGetValue(varNode.DataType.Identifier.ToString(), out nodetype))
                            {
                                var compare = referenceNode.NodeId.ToString() + "[";
                                if (referenceNodes.FirstOrDefault(o => o.NodeId.ToString().StartsWith(compare)) != null)
                                    nodetype += "[]";
                            }
                        }
                        else
                        {
                            nodetype = value.GetType().FullName;
                        }
                    }
                }
                else
                {
                    bool found = false;
                    foreach (var xml in xmls)
                    {
                        foreach (XmlNode value in xml.GetElementsByTagName("opc:StructuredType"))
                        {
                            if (identifier.Substring(3) == value.Attributes["Name"].Value
                                || identifier.Substring(3) == "\"" + value.Attributes["Name"].Value + "\"")
                            {
                                var temp = identifier.Substring(3).Replace("\"", "").Split('.');
                                if (temp.Count() == 1) nodetype = temp[0];
                                else
                                {
                                    nodetype = settings.nameSpacePrefix + String.Join('.' + settings.nameSpacePrefix, temp.Take(temp.Length - 1)) + "." + temp.Last();
                                }
                                var compare = referenceNode.NodeId.ToString() + "[";
                                if (referenceNodes.FirstOrDefault(o => o.NodeId.ToString().StartsWith(compare)) != null) nodetype += "[]";

                                found = true;
                                break;
                            }
                        }
                        if (found) break;
                    }
                    if (!found) continue;
                }
                string nodeId = referenceNode.NodeId.ToString();

                string[] indexItems = GetIndexItems(nodeId);

                string funcName = GetFunctionName(referenceNode.NodeId.Identifier.ToString(), false);
                string nodeName = NodeIdString(nodeId, indexItems);

                var data = new FunctionDefinitions($"ValueNodeEntry<{nodetype}>", funcName, indexItems, nodeName);
                var function = data.Function();
                if (!functionDefinitions.ContainsKey(function))
                {
                    functionDefinitions.Add(function, data);
                }

                var data1 = referenceNode.NodeId.ToString().Replace("\"", "\\\"");


                //fileContents += $"\n\t\tpublic static ValueNodeEntry<{nodetype}> {node}() => new ValueNodeEntry<{nodetype}>(AddressSpace.{node}));";
            }

            foreach (var item in functionDefinitions)
            {

                fileContents += $"\n\t\tpublic static {item.Value.Function()} => new {item.Value.ReturnType}(NodeId.Parse($\"{item.Value.NodeIdString}\"));";
            }


            fileContents += "\n\t}";
            fileContents += "\n}";

            CreateFile(Path.Combine(settings.baseLocation, "NodeEntryAddressSpace.cs"), fileContents);
        }

        private static string[] GetIndexItems(string nodeId)
        {
            return nodeId
                .Replace("\"", "").Split('.').Where(o => o.Contains("[")).Select(o => o.Substring(0, o.IndexOf("["))).ToArray();
        }

        private static string NodeIdString(string nodeId, string[] indexItems)
        {
            var nodeName = nodeId.Replace("\"", "\\\"");
            int counter = 0;
            var index = nodeName.IndexOf('[');

            while (index >= 0)
            {
                var index2 = nodeName.IndexOf(']', index);
                var str1 = nodeName.Substring(0, index + 1);
                var str2 = nodeName.Substring(index2, nodeName.Length - index2);
                nodeName = str1 + "{" + indexItems[counter++] + "}" + str2;
                index = nodeName.IndexOf('[', index2);
            }

            return nodeName;
        }

        private static string GetFunctionName(string nodeId, bool withIndex)
        {
            var funcName = nodeId.Replace(".", "_");
            if (withIndex)
            {
                funcName = Regex.Replace(funcName, "[^0-9a-z_A-Z]|^([^a-z_A-Z]*)", "");
            }
            else
            {
                funcName = Regex.Replace(funcName, "\\[[0-9]*\\]|[^0-9a-z_A-Z]|^([^a-z_A-Z]*)", "");
            }

            var index = funcName.IndexOf('[');
            while (index >= 0)
            {
                var index2 = funcName.IndexOf(']');
                funcName = funcName.Remove(index, index2 - index + 1);
                index = funcName.IndexOf('[');
            }
            return funcName;
        }
    }
}
