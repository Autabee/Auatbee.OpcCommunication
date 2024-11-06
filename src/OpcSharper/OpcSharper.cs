// See https://aka.ms/new-console-template for more information
using Autabee.Communication.ManagedOpcClient;
using Autabee.Communication.ManagedOpcClient.Utilities;
using Autabee.OpcToClass;
using Opc.Ua;
using Opc.Ua.Client;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace Autabee.OpcSharper
{
    public static class OpcSharper
    {
        public static void GenerateProject(AutabeeManagedOpcClient service, GeneratorSettings settings, ILogger logger = null)
        {
            List<Exception> exceptions = new List<Exception>();
            if (settings == null)
                exceptions.Add(new ArgumentNullException(nameof(settings)));
            if (service == null)
            {
                exceptions.Add(new ArgumentNullException(nameof(service)));
                exceptions.Add(new InvalidOperationException("Client is not connected"));
            }
            if (service != null && service.Connected == false)
                exceptions.Add(new InvalidOperationException("Client is not connected"));
            if (settings.nodes == null)
                exceptions.Add(new ArgumentNullException(nameof(settings.nodes)));
            if (exceptions.Count > 0)
                throw new AggregateException(exceptions);


            logger?.Information("Generate Project at, {0}", Path.Combine(Directory.GetCurrentDirectory(), settings.baseLocation));

            OpcToCSharpGenerator.GenerateCsharpProject(settings);


            logger?.Information("Start Retrieving Type Definitions");
            var schemas = service.Session.GetServerTypeSchema();

            XmlDocument[] xmls = new XmlDocument[schemas.Length];
            logger?.Information("Type Definitions Retrieved");
            logger?.Information("Start Generating Type Schema");


            GeneratorDataSet dataSet = new GeneratorDataSet(settings);

            logger?.Information("Gathering Enums");
            for (int i = 0; i < schemas.Length; i++)
            {
                var xmlDocument = new XmlDocument();
                xmlDocument.LoadXml(schemas[i]);
                xmls[i] = xmlDocument;



                var definedEnums = xmlDocument.GetElementsByTagName("opc:EnumeratedType");

                for (int j = 0; j < definedEnums.Count; j++)
                {
                    var enumTemplate = definedEnums[j].ToEnumTemplate(settings.nameSpacePrefix);
                    dataSet.enums.Add(enumTemplate.Name, enumTemplate);
                    enumTemplate.GenerateTypeFile(dataSet);
                }
            }
            logger?.Information("Gathering EncodableObjects");
            for (int i = 0; i < schemas.Length; i++)
            {
                var xmlDocument = new XmlDocument();
                xmlDocument.LoadXml(schemas[i]);
                xmls[i] = xmlDocument;

                var definedStructs = xmlDocument.GetElementsByTagName("opc:StructuredType");

                for (int j = 0; j < definedStructs.Count; j++)
                {
                    var item = definedStructs[j];
                    var typename = OpcToCSharpGenerator.GetCorrectedTypeName(item.Attributes["Name"].Value, settings.nameSpacePrefix);
                    if (dataSet.structs.TryGetValue(typename, out OpcStructTemplate value))
                    {
                        continue;
                    }
                    else
                    {
                        var structTemplate = item.ToStructTemplate(settings.nameSpacePrefix);
                        dataSet.structs.Add(structTemplate.TypeName, structTemplate);
                    }
                }
            }

            logger?.Information("Browse for encodings");
            var task = BrowseForEncodings(service, dataSet, logger);
            task.Wait();



            logger?.Information("Link Base Objects");
            for (int i = 0; i < schemas.Length; i++)
            {
                var xmlDocument = new XmlDocument();
                xmlDocument.LoadXml(schemas[i]);
                xmls[i] = xmlDocument;

                var definedStructs = xmlDocument.GetElementsByTagName("opc:StructuredType");

                for (int j = 0; j < definedStructs.Count; j++)
                {
                    //< opc:StructuredType Name = "TwoWheelerType" BaseType = "s1:VehicleType" >
                    //    < opc:Field Name = "Make" TypeName = "opc:String" SourceType = "s1:VehicleType" />
                    //    < opc:Field Name = "Model" TypeName = "opc:String" SourceType = "s1:VehicleType" />
                    //    < opc:Field Name = "Engine" TypeName = "s1:EngineType" SourceType = "s1:VehicleType" />
                    //    < opc:Field Name = "ManufacturerName" TypeName = "opc:String" />
                    //</ opc:StructuredType >

                    var item = definedStructs[j];
                    // if in node basetype attribute is present link it
                    var basetype = item.Attributes.GetNamedItem("BaseType");
                    if (basetype == null || basetype.Value == "ua:ExtensionObject") continue;

                    var nameType = OpcToCSharpGenerator.GetCorrectedTypeName(item.Attributes["Name"].Value, settings.nameSpacePrefix);
                    var nameBaseType = OpcToCSharpGenerator.GetCorrectedTypeName(basetype.Value, settings.nameSpacePrefix);

                    if (dataSet.structs.TryGetValue(nameType, out OpcStructTemplate currentStruct) && dataSet.structs.TryGetValue(nameBaseType, out OpcStructTemplate baseItem))
                    {
                        currentStruct.BaseType = baseItem;
                    }
                    else
                    {
                        exceptions.Add(new InvalidOperationException($"BaseType {nameBaseType} not found for {nameType}"));
                    }
                }
            }

            if (exceptions.Count > 0)
                throw new AggregateException(exceptions);

            logger?.Information("Generate Objects");
            foreach (var item in dataSet.structs)
            {
                item.Value.GenerateTypeFile(dataSet);
            }



            logger?.Information("Type Schema updated");
            ReferenceDescriptionCollection refdesc = new ReferenceDescriptionCollection();
            ReferenceDescriptionCollection found = new ReferenceDescriptionCollection();
            NodeCollection foundTypes = new NodeCollection();
            foreach (var item in settings.nodes)
            {
                var node = service.BrowseNode(item);
                refdesc.AddRange(node.References);
            };
            found.AddRange(refdesc);


            while (refdesc.Count >= 1)
            {
                var refdisc = new ReferenceDescriptionCollection();

                foreach (var item in refdesc)
                {
                    refdisc.AddRange(service.BrowseNode(item).References);
                }

                refdesc.Clear();
                refdesc.AddRange(refdisc);
                found.AddRange(refdesc);
                logger?.Information($"New Range {refdisc.Count}:");
                logger?.Information(string.Join(Environment.NewLine, refdesc.Select(o => o.NodeId.ToString())));
            }
            found.OrderBy(o => o);
#if NET6_0_OR_GREATER
            foreach (var chunk in found.ToList().Chunk(50))
            {
                var nodes = service.ReadNodes(new ReferenceDescriptionCollection(chunk));
                foundTypes.AddRange(nodes);
            }
#else
            foreach (var chunk in found)
            {
                var nodes = service.ReadNode(chunk);
                foundTypes.Add(nodes);
            }
#endif

            OpcToCSharpGenerator.GenerateAddressSpace(found, settings);
            OpcToCSharpGenerator.GenerateNodeEntryAddressSpace(service, found, foundTypes, xmls, settings);

            try
            {
                string path = GetSharperZip(settings);
                logger.Information("create zip at {0}", path);
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
                ZipFile.CreateFromDirectory(settings.baseLocation, path);
                if (settings.clearOnZip)
                {
                    Directory.Delete(settings.baseLocation, true);
                }
            }
            catch
            {

            }
        }

        private static async Task BrowseForEncodings(AutabeeManagedOpcClient service, GeneratorDataSet dataSet, ILogger logger)
        {
            logger?.Information("Browse for binary type encodings");

            // browse opc binary schema
            service.Session.Browse(
                null,
                null,
                ObjectIds.OPCBinarySchema_TypeSystem,
                0u,
                BrowseDirection.Forward,
                ReferenceTypeIds.HierarchicalReferences,
                true,
                0,
                out var continuationPoint,
                out var refDescColBin);

            foreach (var item in refDescColBin)
            {
                var result = service.Session.Browse(
                    null,
                    null,
                    ((NodeId)item.NodeId),
                    0u,
                    BrowseDirection.Forward,
                    ReferenceTypeIds.HierarchicalReferences,
                    true,
                    0,
                        out continuationPoint,
                        out var types);
                var endoingsToFind = new List<ExpandedNodeId>();
                var structs = new List<OpcStructTemplate>();
                for (int i = 0; i < types.Count; i++)
                {
                    if (dataSet.structs.TryGetValue(types[i].BrowseName.ToString().Split(':').Last(), out var structure))
                    {
                        structure.TypeId = new ExpandedNodeId((NodeId)types[i].NodeId, service.Session.NamespaceUris.GetString(types[i].NodeId.NamespaceIndex));
                        endoingsToFind.Add(structure.TypeId);
                        structs.Add(structure);
                    }
                }
                if (endoingsToFind.Count == 0) continue;
                var results = await FindDefaultBinaryNodes(service.Session, endoingsToFind.Select(n => ExpandedNodeId.ToNodeId(n, service.Session.NamespaceUris)).ToList());

                for (int i = 0; i < results.Count; i++)
                {
                    if (results[i] != null)
                    {
                        structs[i].BinaryEncoding = new ExpandedNodeId((NodeId)results[i], service.Session.NamespaceUris.GetString(results[i].NamespaceIndex));
                    }
                }
            }
            // browse opc xml schema

            service.Session.Browse(
                null,
                null,
                ObjectIds.XmlSchema_TypeSystem,
                0u,
                BrowseDirection.Forward,
                ReferenceTypeIds.HierarchicalReferences,
                true,
                0,
                out continuationPoint,
                out refDescColBin);

            foreach (var item in refDescColBin)
            {
                var result = service.Session.Browse(
                    null,
                    null,
                    ((NodeId)item.NodeId),
                    0u,
                    BrowseDirection.Forward,
                    ReferenceTypeIds.HierarchicalReferences,
                    true,
                    0,
                        out continuationPoint,
                        out var types);
                var endoingsToFind = new List<ExpandedNodeId>();
                var structs = new List<OpcStructTemplate>();
                for (int i = 0; i < types.Count; i++)
                {
                    if (dataSet.structs.TryGetValue(types[i].BrowseName.ToString().Split(':').Last(), out var structure))
                    {
                        if (structure.TypeId != null)
                        {
                            endoingsToFind.Add(types[i].NodeId);
                            structs.Add(structure);
                            continue;
                        }
                        structure.TypeId = new ExpandedNodeId((NodeId)types[i].NodeId, service.Session.NamespaceUris.GetString(types[i].NodeId.NamespaceIndex));
                        endoingsToFind.Add(structure.TypeId);
                        structs.Add(structure);
                    }
                }
                if (endoingsToFind.Count == 0) continue;
                var results = await FindDefaultXMLNodes(service.Session, endoingsToFind.Select(n => (NodeId)n).ToList());

                for (int i = 0; i < results.Count; i++)
                {
                    if (results[i] != null)
                    {
                        structs[i].XmlEncoding = new ExpandedNodeId((NodeId)results[i], service.Session.NamespaceUris.GetString(results[i].NamespaceIndex));
                    }
                }
            }
        }
        private static async Task<ExpandedNodeIdCollection> FindDefaultBinaryNodes(Session session, List<NodeId> nodes)
           => await FindDefaultObjectNodes(session, nodes, new string[] { "Default Binary", "DefaultBinary" });
        private static async Task<ExpandedNodeIdCollection> FindDefaultXMLNodes(Session session, List<NodeId> nodes)
          => await FindDefaultObjectNodes(session, nodes, new string[] { "Default XML", "DefaultXML" });
        private static async Task<ExpandedNodeIdCollection> FindDefaultObjectNodes(Session session, List<NodeId> nodes, string[] names)
        {
            if (names == null)
            {
                throw new ArgumentNullException(nameof(names));
            }

            var collection = new BrowseDescriptionCollection(nodes.Count);

            for (int i = 0; i < nodes.Count; i++)
            {
                collection.Add(new BrowseDescription
                {
                    NodeId = nodes[i],
                    BrowseDirection = BrowseDirection.Inverse,
                    IncludeSubtypes = true,
                    NodeClassMask = (uint)(NodeClass.Object),
                    ResultMask = (uint)BrowseResultMask.All
                });
            }

            var result = await session.BrowseAsync(null, null, 0, collection, CancellationToken.None);

            var results = new ExpandedNodeIdCollection(nodes.Count);

            for (int i = 0; i < result.Results.Count; i++)
            {
                results.Add(null);
                for (global::System.Int32 j = 0; j < result.Results[i].References.Count; j++)
                {
                    var reference = result.Results[i].References[j];
                    if (names.Any(n => reference.BrowseName.Name.Contains(n)))
                    {
                        results[i] = reference.NodeId;
                    }
                }
            }
            return results;
        }

        public static async Task<ExtensionObject> ReadDefaultBinaryNode(Session session, NodeId nodeId)
        {
            // Read the raw binary data from the node
            DataValue value = await session.ReadValueAsync(nodeId);
            if (value.Value is byte[] binaryData)
            {
                // Decode binary data into an ExtensionObject
                using (MemoryStream stream = new MemoryStream(binaryData))
                using (BinaryDecoder decoder = new BinaryDecoder(stream, session.MessageContext))
                {
                    return decoder.ReadExtensionObject(null);
                }
            }
            return null;
        }

        public static void ExtractNodeIdsFromExtensionObject(ExtensionObject extensionObject)
        {
            if (extensionObject.Body is IEncodeable encodeable)
            {
                // You can reflect on encodeable or use specific properties
                var typeId = extensionObject.TypeId;
                Console.WriteLine("Type NodeId: " + typeId);

                // Use reflection to inspect properties or fields of the IEncodeable object
                foreach (var property in encodeable.GetType().GetProperties())
                {
                    Console.WriteLine($"Property Name: {property.Name}, Value: {property.GetValue(encodeable)}");
                }
            }
            else
            {
                Console.WriteLine("Failed to decode as IEncodeable type.");
            }
        }

        //private static async void TestSesion(Session session)
        //{
        //    // Start at ObjectsFolder or another relevant root
        //    var encodingNodes = new List<NodeId>() { new NodeId(335, 3) };
        //    var results = await FindDefaultObjectNodes(session, encodingNodes);

        //    for (int i = 0; i < results.Count; i++)
        //    {
        //        Console.WriteLine($"Encoding NodeId for DataType {encodingNodes[i]}: {results[i]}");
        //    }
        //}

        public static string GetSharperZip(GeneratorSettings settings)
        {
            Directory.CreateDirectory(settings.zipStoreLocation);
            var path = Path.Combine(settings.zipStoreLocation, settings.baseNamespace + ".zip");
            return path;
        }
    }
}