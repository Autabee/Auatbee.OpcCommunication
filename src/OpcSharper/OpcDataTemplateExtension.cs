using System;
using System.IO;
using System.Linq;
using System.Xml;

namespace Autabee.OpcToClass
{
    public static class OpcDataTemplateExtension
    {

        public static void GenerateTypeFile(this IOpcSharperTemplate data, GeneratorDataSet settings)
        {
            Directory.CreateDirectory(settings.baseLocation);

            var (name, scriptContent) = (data.Name, data.GetScriptAsFile(settings));

            var namespaceName = data.NameSpace;
            var className = data.ClassName;
            var fileName = className + ".cs";

            string filePath;
            if (string.IsNullOrWhiteSpace(namespaceName))
            {
                filePath = Path.Combine(settings.baseLocation, fileName);
            }
            else
            {
                Directory.CreateDirectory(Path.Combine(settings.baseLocation, settings.nameSpacePrefix + string.Join($"\\{settings.nameSpacePrefix}", className)));
                filePath = Path.Combine(settings.baseLocation, settings.nameSpacePrefix + string.Join($"\\{settings.nameSpacePrefix}", className), fileName);
            }

            OpcToCSharpGenerator.CreateFile(filePath, scriptContent);
        }

        public static Field[] GetFields(this OpcStructTemplate template)
        {
            Field[] fields = template.Fields;
            if (template.BaseType != null)
            {
                fields = fields.Concat(template.BaseType.GetFields()).ToArray();
            }
            return fields;
        }

        private static Field[] GetClassOnlyFields(this OpcStructTemplate template)
        {
            var fields = template.Fields;
            if (template.BaseType != null)
            {
                // remove fields that are already in the base type
                foreach (var field in template.BaseType.GetFields())
                {
                    fields = fields.Where(o => o.Name != field.Name).ToArray();
                }
            }

            return fields;
        }

        public static OpcEnumTemplate ToEnumTemplate(this XmlNode node, string nsPrefix)
        {
            OpcEnumTemplate template = new OpcEnumTemplate();
            template.Name = OpcToCSharpGenerator.GetCorrectedName(node);
            //template.TypeName = OpcToCSharpGenerator.GetCorrectedTypeName(node, nsPrefix);
            template.NamespaceURI = node.NamespaceURI;

            foreach (XmlNode field in node.ChildNodes)
            {
                if (field.Name == "opc:EnumeratedValue")
                {
                    template.Fields = template.Fields.Append(new Field()
                    {
                        Name = OpcToCSharpGenerator.GetCorrectedName(field),
                        Type = string.Empty,
                        TypeName = string.Empty,
                        Value = field.Attributes["Value"].Value
                    }).ToArray();
                }
            }

            return template;
        }

        public static OpcStructTemplate ToStructTemplate(this XmlNode node, string nsPrefix)
        {
            OpcStructTemplate template = new OpcStructTemplate();

            template.nodes = new XmlNode[] { node };
            template.Name = OpcToCSharpGenerator.GetCorrectedName(node);
            template.TypeName = OpcToCSharpGenerator.GetCorrectedTypeName(node.Attributes["Name"].Value, nsPrefix);
            template.NamespaceURI = node.NamespaceURI;

            for (int i = 0; i < node.ChildNodes.Count; i++)
            {
                XmlNode field = node.ChildNodes[i];
                if (field.Name == "opc:Field")
                {
                    var fieldName = OpcToCSharpGenerator.GetCorrectedName(field);
                    var fieldType = OpcToCSharpGenerator.GetCorrectedTypeName(field, nsPrefix);

                    Field newField = new Field()
                    {
                        Name = fieldName,
                        Type = fieldType,
                        TypeName = field.Attributes["TypeName"]?.Value,
                        Value = field.Attributes["Value"]?.Value
                    };

                    template.Fields = template.Fields.Append(newField).ToArray();
                }
            }



            return template;
        }


        public static string GenerateClassScript(this OpcStructTemplate template, GeneratorDataSet dataSet)
        {
            string[] enums = dataSet.enums.Keys.ToArray();
            if (enums == null) enums = new string[0];

            string ns = string.IsNullOrWhiteSpace(template.NameSpace)
                ? dataSet.baseNamespace
                : $"{dataSet.baseNamespace}.{template.NameSpace}";


            string classData = $"\nnamespace {ns}\n{{";
            if (template.BaseType == null)
                classData += $"\n\tpublic class {template.Name} : EncodeableObject\n\t{{";
            else
                classData += $"\n\tpublic class {template.Name} : {template.BaseType.Name}\n\t{{";

            Field[] fields = template.GetClassOnlyFields();

            foreach (var field in fields)
            {
                classData += $"\n\t\tpublic {field.Type} {field.Name} {{ get; set; }}";
            }

            // To Do This does not work always as systems somtimes use struct name with or without qoutations.
            // example: `"value"` vs `value`.
            // might need to read the node itself to get the correct name.

            var getStringOfIdentifier = (object id) => id.GetType() == typeof(string)
                ? $"\"{id}\""
                : $"{id}";


            classData += template.TypeId != null
                ? $"\n\t\tprivate static ExpandedNodeId typeId = new ExpandedNodeId({getStringOfIdentifier(template.TypeId.Identifier)},\"{template.TypeId.NamespaceUri}\");"
                + $"\n\t\tpublic override ExpandedNodeId TypeId => typeId;"
                : $"\n\t\tpublic override ExpandedNodeId TypeId => NodeId.Null;";

            classData += template.BinaryEncoding != null
                ? $"\n\t\tprivate static ExpandedNodeId binaryEncodingId = new ExpandedNodeId({getStringOfIdentifier(template.BinaryEncoding.Identifier)},\"{template.BinaryEncoding.NamespaceUri}\");"
                + $"\n\t\tpublic override ExpandedNodeId BinaryEncodingId => binaryEncodingId;"
                : $"\n\t\tpublic override ExpandedNodeId BinaryEncodingId => NodeId.Null;";


            classData += template.XmlEncoding != null
                ? $"\n\t\tprivate static ExpandedNodeId xmlNodeId = new ExpandedNodeId({getStringOfIdentifier(template.XmlEncoding.Identifier)},\"{template.XmlEncoding.NamespaceUri}\");"
                + $"\n\t\tpublic override ExpandedNodeId  XmlEncodingId => xmlNodeId;"
                : $"\n\t\tpublic override ExpandedNodeId XmlEncodingId => NodeId.Null;";

            //Encoder function
            string encoder = "encoder";
            classData += GetEncoderFunction(template.BaseType, enums, fields, encoder);


            //decoder function
            string decoder = "decoder";
            classData += GetDecoderFunction(template.BaseType, enums, fields, decoder);

            classData += "\n\t}\n}";

            return classData;
        }



        private static string GetDecoderFunction(this OpcStructTemplate template, string[] enums, string decoder)
            => GetDecoderFunction(template.BaseType, enums, template.GetClassOnlyFields(), decoder);


        private static string GetDecoderFunction(OpcStructTemplate baseTemplate, string[] enums, Field[] fields, string decoder)
        {
            string classData = string.Empty;
            classData += $"\n\n\tpublic override void Decode(IDecoder {decoder})\n\t{{";

            if (baseTemplate != null)
            {
                classData += $"\n\t\tbase.Decode({decoder});";
            }

            foreach (var field in fields)
            {
                classData += $"\n\t\tthis.{field.Name} = {OpcToCSharpGenerator.GetDecoder(field, enums, decoder)};";
            }

            classData += "\n\t}";
            return classData;
        }

        private static string GetEncoderFunction(this OpcStructTemplate template, string[] enums, string encoder)
            => GetEncoderFunction(template.BaseType, enums, template.GetClassOnlyFields(), encoder);

        private static string GetEncoderFunction(OpcStructTemplate baseTemplate, string[] enums, Field[] fields, string encoder)
        {
            string classData = string.Empty;
            classData += $"\n\n\tpublic override void Encode(IEncoder {encoder})\n\t{{";

            if (baseTemplate != null)
            {
                classData += $"\n\t\tbase.Encode({encoder});";
            }

            foreach (var field in fields)
            {
                classData += $"\n\t\t{OpcToCSharpGenerator.GetEncoder(field, enums, encoder)};";
            }
            classData += "\n\t}";
            return classData;
        }
    }
}
